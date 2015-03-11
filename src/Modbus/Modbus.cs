using System;
namespace Modbus
{	
	/// <summary>
	///  Defines constants related to the Modbus protocol.
	/// </summary>
    public static class Modbus
	{			
		// supported function codes
		/// <summary>
		/// 
		/// </summary>
        public const byte ReadCoils = 1;
        /// <summary>
        /// 
        /// </summary>
        public const byte ReadInputs = 2;
        /// <summary>
        /// 
        /// </summary>
        public const byte ReadHoldingRegisters = 3;
        /// <summary>
        /// 
        /// </summary>
        public const byte ReadInputRegisters = 4;
        /// <summary>
        /// 
        /// </summary>
        public const byte WriteSingleCoil = 5;
        /// <summary>
        /// 
        /// </summary>
        public const byte WriteSingleRegister = 6;
        /// <summary>
        /// 
        /// </summary>
        public const byte Diagnostics = 8;
        /// <summary>
        /// 
        /// </summary>
        public const ushort DiagnosticsReturnQueryData = 0;
        /// <summary>
        /// 
        /// </summary>
        public const byte WriteMultipleCoils = 15;
        /// <summary>
        /// 
        /// </summary>
        public const byte WriteMultipleRegisters = 16;
        /// <summary>
        /// 
        /// </summary>
	    public const byte ReadFiles = 20;
        /// <summary>
        /// 
        /// </summary>
		public const byte ReadWriteMultipleRegisters = 23;

        /// <summary>
        /// 
        /// </summary>
        public const int MaximumDiscreteRequestResponseSize = 2040;
        /// <summary>
        /// 
        /// </summary>
        public const int MaximumRegisterRequestResponseSize = 127;

        /// <summary>
        /// modbus slave exception offset that is added to the function code, to flag an exception
        /// </summary>
		public const byte ExceptionOffset = 128;

        // modbus slave exception codes
        /// <summary>
        ///
        /// </summary>
        public const byte Acknowledge = 5;
        /// <summary>
        /// 
        /// </summary>
		public const byte SlaveDeviceBusy = 6;

        /// <summary>
        /// default setting for number of retries for IO operations
        /// </summary>
		public const int DefaultRetries = 3;

        /// <summary>
        /// default number of milliseconds to wait after encountering an ACKNOWLEGE or SLAVE DEVIC BUSY slave exception response.
        /// </summary>
		public const int DefaultWaitToRetryMilliseconds = 250;

        /// <summary>
        /// default setting for IO timeouts in milliseconds
        /// </summary>
		[Obsolete("Default timeout for read write operations is infinite.")]
        public const int DefaultTimeout = 1000;

        /// <summary>
        /// smallest supported message frame size (sans checksum)
        /// </summary>
		public const int MinimumFrameSize = 2;

        /// <summary>
        /// 
        /// </summary>
        public const ushort CoilOn = 0xFF00;
        /// <summary>
        /// 
        /// </summary>
		public const ushort CoilOff = 0x0000;

        /// <summary>
        /// IP slaves should be addressed by IP
        /// </summary>
		public const byte DefaultIpSlaveUnitId = 0;

        /// <summary>
        /// An existing connection was forcibly closed by the remote host
        /// </summary>
		public const int ConnectionResetByPeer = 10054;

        /// <summary>
        /// Existing socket connection is being closed
        /// </summary>
		public const int WSACancelBlockingCall = 10004;

        /// <summary>
        /// used by the ASCII tranport to indicate end of message
        /// </summary>
		public const string NewLine = "\r\n";
	}
}
