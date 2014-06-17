using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Modbus.Data;
using Unme.Common;

namespace Modbus.Message
{
    /// <summary>
    /// 
    /// </summary>
    public class ReadWriteMultipleRegistersRequest : ModbusMessage, IModbusRequest
	{		
		private ReadHoldingInputRegistersRequest _readRequest;
		private WriteMultipleRegistersRequest _writeRequest;

		/// <summary>
		/// 
		/// </summary>
		public ReadWriteMultipleRegistersRequest()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="slaveAddress"></param>
		/// <param name="startReadAddress"></param>
		/// <param name="numberOfPointsToRead"></param>
		/// <param name="startWriteAddress"></param>
		/// <param name="writeData"></param>
		public ReadWriteMultipleRegistersRequest(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, RegisterCollection writeData)
			: base(slaveAddress, Modbus.ReadWriteMultipleRegisters)
		{
			_readRequest = new ReadHoldingInputRegistersRequest(Modbus.ReadHoldingRegisters, slaveAddress, startReadAddress, numberOfPointsToRead);
			_writeRequest = new WriteMultipleRegistersRequest(slaveAddress, startWriteAddress, writeData);
		}

        /// <summary>
        /// 
        /// </summary>
		public override byte[] ProtocolDataUnit
		{
			get
			{
				// read and write PDUs without function codes
				byte[] read = _readRequest.ProtocolDataUnit.Slice(1, _readRequest.ProtocolDataUnit.Length - 1).ToArray();
				byte[] write = _writeRequest.ProtocolDataUnit.Slice(1, _writeRequest.ProtocolDataUnit.Length - 1).ToArray();

				return FunctionCode.ToSequence().Concat(read, write).ToArray();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public ReadHoldingInputRegistersRequest ReadRequest
		{
			get { return _readRequest; }
		}

		/// <summary>
		/// 
		/// </summary>
		public WriteMultipleRegistersRequest WriteRequest
		{
			get { return _writeRequest; }
		}

        /// <summary>
        /// 
        /// </summary>
		public override int MinimumFrameSize
		{
			get { return 11; }
		}

        /// <summary>
        /// 
        /// </summary>
		public override string ToString()
		{
			return String.Format(CultureInfo.InvariantCulture, 
				"Write {0} holding registers starting at address {1}, and read {2} registers starting at address {3}.",
				_writeRequest.NumberOfPoints, 
				_writeRequest.StartAddress, 
				_readRequest.NumberOfPoints, 
				_readRequest.StartAddress);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="response"></param>
		/// <exception cref="IOException"></exception>
		public void ValidateResponse(IModbusMessage response)
		{
			var typedResponse = (ReadHoldingInputRegistersResponse) response;

			var expectedByteCount = ReadRequest.NumberOfPoints * 2;
			if (expectedByteCount != typedResponse.ByteCount)
			{
				throw new IOException(String.Format(CultureInfo.InvariantCulture,
					"Unexpected byte count in response. Expected {0}, received {1}.", 
					expectedByteCount, 
					typedResponse.ByteCount));
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
		protected override void InitializeUnique(byte[] frame)
		{
			if (frame.Length < MinimumFrameSize + frame[10])
				throw new FormatException("Message frame does not contain enough bytes.");

			byte[] readFrame = frame.Slice(2, 4).ToArray();
			byte[] writeFrame = frame.Slice(6, frame.Length - 6).ToArray();
			byte[] header = { SlaveAddress, FunctionCode };

			_readRequest = ModbusMessageFactory.CreateModbusMessage<ReadHoldingInputRegistersRequest>(header.Concat(readFrame).ToArray());
			_writeRequest = ModbusMessageFactory.CreateModbusMessage<WriteMultipleRegistersRequest>(header.Concat(writeFrame).ToArray());
		}
	}
}
