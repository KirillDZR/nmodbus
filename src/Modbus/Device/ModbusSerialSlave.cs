using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using Modbus.IO;
using Modbus.Message;
using Modbus.Unme.Common;
using NLog;


namespace Modbus.Device
{
	/// <summary>
	/// Modbus serial slave device.
	/// </summary>
	public class ModbusSerialSlave : ModbusSlave
	{
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private ModbusSerialSlave(byte unitId, ModbusTransport transport)
            : base(unitId, transport)
		{
		}

		private ModbusSerialTransport SerialTransport
		{
			get
			{
				var transport = Transport as ModbusSerialTransport;

				if (transport == null)
					throw new ObjectDisposedException("SerialTransport");

				return transport;
			}
		}

		/// <summary>
		/// Modbus ASCII slave factory method.
		/// </summary>
        public static ModbusSerialSlave CreateAscii(byte unitId, SerialPort serialPort)
		{
			if (serialPort == null)
				throw new ArgumentNullException("serialPort");

            return CreateAscii(unitId, new SerialPortAdapter(serialPort));
		}

		/// <summary>
		/// Modbus ASCII slave factory method.
		/// </summary>
		public static ModbusSerialSlave CreateAscii(byte unitId, IStreamResource streamResource)
		{
			if (streamResource == null)
				throw new ArgumentNullException("streamResource");

            return new ModbusSerialSlave(unitId, new ModbusAsciiTransport(streamResource));
		}

		/// <summary>
		/// Modbus RTU slave factory method.
		/// </summary>
        public static ModbusSerialSlave CreateRtu(byte unitId, SerialPort serialPort)
		{
			if (serialPort == null)
				throw new ArgumentNullException("serialPort");

            return CreateRtu(unitId, new SerialPortAdapter(serialPort));
		}

		/// <summary>
		/// Modbus RTU slave factory method.
		/// </summary>
        public static ModbusSerialSlave CreateRtu(byte unitId, IStreamResource streamResource)
		{
			if (streamResource == null)
				throw new ArgumentNullException("streamResource");

            return new ModbusSerialSlave(unitId, new ModbusRtuTransport(streamResource));
		}

	    /// <summary>
	    /// Start slave listening for requests.
	    /// </summary>
	    public override void Listen()
	    {
            while (!Cts.Token.IsCancellationRequested)
	        {
	            try
	            {
	                try
	                {
	                    // read request and build message
                        var frame = SerialTransport.ReadRequest(this);

                        if (frame == null || frame.Length == 0) continue;

                        Logger.Info("RX: {0}", frame.Join(", "));

	                    IModbusMessage request = ModbusMessageFactory.CreateModbusRequest(this, frame);

	                    if (SerialTransport.CheckFrame && !SerialTransport.ChecksumsMatch(request, frame))
	                    {
                            var errorMessage = String.Format(CultureInfo.InvariantCulture,
	                            "Checksums failed to match {0} != {1}", request.MessageFrame.Join(", "), frame.Join(", "));
	                        Logger.Error(errorMessage);
	                        throw new IOException(errorMessage);
	                    }

	                    // only service requests addressed to this particular slave
	                    if (request.SlaveAddress != UnitId)
	                    {
	                        Logger.Debug(
	                            "NModbus Slave {0} ignoring request intended for NModbus Slave {1}", UnitId,
	                            request.SlaveAddress);
	                        continue;
	                    }

	                    // perform action
	                    IModbusMessage response = ApplyRequest(request);

	                    // write response
	                    SerialTransport.Write(response);
	                }
	                catch (IOException ioe)
	                {
	                    Logger.Error("IO Exception encountered while listening for requests - {0}", ioe.Message);
	                    SerialTransport.DiscardInBuffer();
	                }
	                catch (TimeoutException te)
	                {
	                    Logger.Error("Timeout Exception encountered while listening for requests - {0}", te.Message);
	                    SerialTransport.DiscardInBuffer();
	                }

	                // TODO better exception handling here, missing FormatException, NotImplemented...
	            }
			    //catch (InvalidOperationException)
			    //{
			    //    // when the underlying transport is disposed
			    //    break;
			    //}
	            catch (Exception e)
	            {
	                Logger.Error("Exception - {0}", e.Message);
			        //SerialTransport.Write(new SlaveExceptionResponse());
                    SerialTransport.DiscardInBuffer();
                    //throw;
	            }
	        }
	    }
	}
}
