using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Modbus.Data;

namespace Modbus.Message
{
	/// <summary>
	/// Class holding all implementation shared between two or more message types. 
	/// Interfaces expose subsets of type specific implementations.
	/// </summary>
    public class ModbusMessageImpl
	{
		/// <summary>
		/// 
		/// </summary>
		public ModbusMessageImpl()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="slaveAddress"></param>
		/// <param name="functionCode"></param>
		public ModbusMessageImpl(byte slaveAddress, byte functionCode)
		{
			SlaveAddress = slaveAddress;
			FunctionCode = functionCode;
		}

		/// <summary>
		/// 
		/// </summary>
		public byte? ByteCount { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public byte? ExceptionCode { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ushort TransactionId { get; set; }
		
		/// <summary>
		/// 
		/// </summary>
		public byte FunctionCode { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ushort? NumberOfPoints { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public byte SlaveAddress { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ushort? StartAddress { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ushort? SubFunctionCode { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public IModbusMessageDataCollection Data { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public byte[] MessageFrame
		{
			get
			{
				List<byte> frame = new List<byte>();
				frame.Add(SlaveAddress);
				frame.AddRange(ProtocolDataUnit);

				return frame.ToArray();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public byte[] ProtocolDataUnit
		{
			get
			{
				List<byte> pdu = new List<byte>();

				pdu.Add(FunctionCode);

				if (ExceptionCode.HasValue)
					pdu.Add(ExceptionCode.Value);

				if (SubFunctionCode.HasValue)
					pdu.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) SubFunctionCode.Value)));

				if (StartAddress.HasValue)
					pdu.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) StartAddress.Value)));

				if (NumberOfPoints.HasValue)
					pdu.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) NumberOfPoints.Value)));

				if (ByteCount.HasValue)
					pdu.Add(ByteCount.Value);

				if (Data != null)
					pdu.AddRange(Data.NetworkBytes);

				return pdu.ToArray();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="FormatException"></exception>
		public void Initialize(byte[] frame)
		{
			if (frame == null)
				throw new ArgumentNullException("frame", "Argument frame cannot be null.");

			if (frame.Length < Modbus.MinimumFrameSize)
				throw new FormatException(String.Format(CultureInfo.InvariantCulture, "Message frame must contain at least {0} bytes of data.", Modbus.MinimumFrameSize));

			SlaveAddress = frame[0];
			FunctionCode = frame[1];
		}
	}
}
