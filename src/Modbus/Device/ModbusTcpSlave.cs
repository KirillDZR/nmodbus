using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using NLog;
using Modbus.IO;

namespace Modbus.Device
{
	/// <summary>
	/// Modbus TCP slave device.
	/// </summary>
	public class ModbusTcpSlave : ModbusSlave
	{
		private readonly object _mastersLock = new object();
		private readonly object _serverLock = new object();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private readonly Dictionary<string, ModbusMasterTcpConnection> _masters = new Dictionary<string, ModbusMasterTcpConnection>();
		private TcpListener _server;

		private ModbusTcpSlave(byte unitId, TcpListener tcpListener)
			: base(unitId, new EmptyTransport())
		{
			if (tcpListener == null)
				throw new ArgumentNullException("tcpListener");

			_server = tcpListener;
		}

		/// <summary>
		/// Gets the Modbus TCP Masters connected to this Modbus TCP Slave.
		/// </summary>
		public ReadOnlyCollection<TcpClient> Masters
		{
			get
			{
				lock (_mastersLock)
					return new ReadOnlyCollection<TcpClient>(_masters.Values.Select(mc => mc.TcpClient).ToList());
			}
		}

		/// <summary>
		/// Gets the server.
		/// </summary>
		/// <value>The server.</value>
		/// <remarks>
		/// This property is not thread safe, it should only be consumed within a lock.
		/// </remarks>
		private TcpListener Server
		{
			get
			{
				if (_server == null)
					throw new ObjectDisposedException("Server");

				return _server;
			}
		}

		/// <summary>
		/// Modbus TCP slave factory method.
		/// </summary>
		public static ModbusTcpSlave CreateTcp(byte unitId, TcpListener tcpListener)
		{
			return new ModbusTcpSlave(unitId, tcpListener);
		}

		/// <summary>
		/// Start slave listening for requests.
		/// </summary>
		public override void Listen()
		{
            //Logger.Debug("Start Modbus Tcp Server.");

			lock (_serverLock)
			{
				try
				{
					Server.Start();

				    // use Socket async API for compact framework compat
                    Server.Server.BeginAccept(AcceptCompleted, this);
				}
				catch (ObjectDisposedException)
				{
					// this happens when the server stops
                }
			}
		}

		internal void RemoveMaster(string endPoint)
		{
			lock (_mastersLock)
			{
				if (!_masters.Remove(endPoint))
					throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "EndPoint {0} cannot be removed, it does not exist.", endPoint));
			}

            Logger.Info("Removed Master {0}", endPoint);
		}

		internal void AcceptCompleted(IAsyncResult ar)
		{
			var slave = (ModbusTcpSlave) ar.AsyncState;

		    try
		    {
		        // use Socket async API for compact framework compat
		        Socket socket;
		        lock (_serverLock)
		            socket = Server.Server.EndAccept(ar);

		        var client = new TcpClient {Client = socket};
		        var masterConnection = new ModbusMasterTcpConnection(client, slave);
		        masterConnection.ModbusMasterTcpConnectionClosed += (sender, eventArgs) => RemoveMaster(eventArgs.EndPoint);

		        lock (_mastersLock)
		            _masters.Add(client.Client.RemoteEndPoint.ToString(), masterConnection);

                Logger.Debug("Accept completed.");
            }
		    catch (ObjectDisposedException)
		    {
		        // this happens when the server stops
            }
            catch (Exception e)
            {
                Logger.Error("Exception", e);
            }

            if (Cts.Token.IsCancellationRequested) return;

            // Accept another client
            // use Socket async API for compact framework compat
            lock (_serverLock)
                Server.Server.BeginAccept(AcceptCompleted, slave);
        }

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		/// <remarks>Dispose is thread-safe.</remarks>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// double-check locking
				if (_server != null)
				{
					lock (_serverLock)
					{
						if (_server != null)
						{
							_server.Stop();
							_server = null;
						}
					}
				}
			}
		}
	}
}
