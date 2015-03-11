using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Modbus.IO;
using Modbus.Message;
using Modbus.Unme.Common;
using NLog;

namespace Modbus.Device
{
	internal class ModbusMasterTcpConnection : ModbusDevice//, IDisposable
	{
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private readonly TcpClient _client;		
		private readonly string _endPoint;
		private readonly Stream _stream;
		private readonly ModbusTcpSlave _slave;
        //private static int _instanceCounter;
		private byte[] _mbapHeader = new byte[6];
		private byte[] _messageFrame;

		public ModbusMasterTcpConnection(TcpClient client, ModbusTcpSlave slave)
			: base(new ModbusIpTransport(new TcpClientAdapter(client)))
		{
			if (client == null)
				throw new ArgumentNullException("client");
			if (slave == null)
				throw new ArgumentException("slave");

			_client = client;
			_endPoint = client.Client.RemoteEndPoint.ToString();
			_stream = client.GetStream();
			_slave = slave;
            Logger.Debug("Creating new Master connection at IP:{0}", EndPoint);

            Logger.Debug("Begin reading header.");
			Stream.BeginRead(_mbapHeader, 0, 6, ReadHeaderCompleted, null);
		}

		/// <summary>
		/// Occurs when a Modbus master TCP connection is closed.
		/// </summary>
		public event EventHandler<TcpConnectionEventArgs> ModbusMasterTcpConnectionClosed;

		public string EndPoint
		{
			get
			{
				return _endPoint;
			}
		}

		public ModbusSlave Slave
		{
			get
			{
				return _slave;
			}
		}

		public Stream Stream
		{
			get
			{
				return _stream;
			}
		}

		public TcpClient TcpClient
		{
			get
			{
				return _client;
			}
		}

		internal void ReadHeaderCompleted(IAsyncResult ar)
		{
            Logger.Debug("Read header completed.");

			CatchExceptionAndRemoveMasterEndPoint(() =>
			{
				// this is the normal way a master closes its connection
				if (Stream.EndRead(ar) == 0)
				{
                    Logger.Debug("0 bytes read, Master has closed Socket connection.");
					ModbusMasterTcpConnectionClosed.Raise(this, new TcpConnectionEventArgs(EndPoint));
					return;
				}

                Logger.Debug("MBAP header: {0}", _mbapHeader.Join(", "));
				var frameLength = (ushort) IPAddress.HostToNetworkOrder(BitConverter.ToInt16(_mbapHeader, 4));
                Logger.Debug("{0} bytes in PDU.", frameLength);
				_messageFrame = new byte[frameLength];

				Stream.BeginRead(_messageFrame, 0, frameLength, ReadFrameCompleted, null);
			}, EndPoint);
		}

		internal void ReadFrameCompleted(IAsyncResult ar)
		{
			CatchExceptionAndRemoveMasterEndPoint(() =>
			{
                Logger.Debug("Read Frame completed {0} bytes", Stream.EndRead(ar));
                var frame = _mbapHeader.Concat(_messageFrame).ToArray();
                Logger.Info("RX: {0}", frame.Join(", "));

                var request = ModbusMessageFactory.CreateModbusRequest(Slave, frame.Slice(6, frame.Length - 6).ToArray());
				request.TransactionId = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 0));

				// perform action and build response
                var response = Slave.ApplyRequest(request);
				response.TransactionId = request.TransactionId;

				// write response
                var responseFrame = Transport.BuildMessageFrame(response);
                Logger.Info("TX: {0}", responseFrame.Join(", "));
				Stream.BeginWrite(responseFrame, 0, responseFrame.Length, WriteCompleted, null);
			}, EndPoint);
		}

		internal void WriteCompleted(IAsyncResult ar)
		{
            Logger.Debug("End write.");

			CatchExceptionAndRemoveMasterEndPoint(() =>
			{
				Stream.EndWrite(ar);
                Logger.Debug("Begin reading another request.");
				Stream.BeginRead(_mbapHeader, 0, 6, ReadHeaderCompleted, null);
			}, EndPoint);
		}

		/// <summary>
		/// Catches all exceptions thrown when action is executed and removes the master end point.
		/// The exception is ignored when it simply signals a master closing its connection.
		/// </summary>
		internal void CatchExceptionAndRemoveMasterEndPoint(Action action, string endPoint)
		{
			if (action == null)
				throw new ArgumentNullException("action");
			if (endPoint == null)
				throw new ArgumentNullException("endPoint");
			if (endPoint.IsNullOrEmpty())
				throw new ArgumentException("Argument endPoint cannot be empty.");

			try
			{
				action.Invoke();
			}
			catch (IOException ioe)
			{
                Logger.Debug("IOException encountered in ReadHeaderCompleted - {0}", ioe.Message);
				ModbusMasterTcpConnectionClosed.Raise(this, new TcpConnectionEventArgs(EndPoint));

				var socketException = ioe.InnerException as SocketException;
				if (socketException != null && socketException.ErrorCode == Modbus.ConnectionResetByPeer)
				{
                    Logger.Debug("Socket Exception ConnectionResetByPeer, Master closed connection.");
				}

                Logger.Debug("Socket Exception, Master closed connection.");
			}
			catch (Exception e)
			{
                Logger.Error("Unexpected exception encountered", e);
                var responseFrame = Transport.BuildMessageFrame(new SlaveExceptionResponse());
                Logger.Info("ERROR TX: {0}", responseFrame.Join(", "));
                Stream.BeginWrite(responseFrame, 0, responseFrame.Length, WriteCompleted, null);
			}
		}
	}
}
