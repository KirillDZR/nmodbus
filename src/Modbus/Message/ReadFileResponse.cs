using System;
using System.Collections.Generic;

namespace Modbus.Message
{
    /// <summary>
    /// 
    /// </summary>
    public class ReadFileResponse : IModbusMessage // IModbusMessageRtu
    {
        // public Func<byte[], int> RtuBytesRemaining
        //  {
        //      get
        //      {
        //          return frameStart => frameStart[2] + 1;
        //      }
        // }

        private readonly List<byte> _data;

        /// <summary>
        /// 
        /// </summary>
        public ReadFileResponse()
        {
            _data = new List<byte>();
            FunctionCode = Modbus.ReadFiles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"></param>
        public void AddRecordData(byte[] buffer, int count)
        {
            // File Resp. Length
            _data.Add((byte)(count + 1));
            // Reference Type
            _data.Add(0x06);
            // Record Data
            for (var i = 0; i < count; i++)
            {
                _data.Add(buffer[i]); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Data
        {
            get { return _data.ToArray(); }
            set { _data.AddRange(value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte ByteCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte FunctionCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte SlaveAddress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte[] MessageFrame
        {
            get
            {
                var frame = new List<byte>
                {
                    SlaveAddress
                };
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
                var pdu = new List<byte>
                {
                    FunctionCode, 
                    (byte) Data.Length // Resp. data Length
                };
                pdu.AddRange(Data);

                return pdu.ToArray();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort TransactionId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void Initialize(byte[] frame)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");

            if (frame.Length < 3 || frame.Length < 3 + frame[2])
                throw new ArgumentException(@"Message frame does not contain enough bytes.", "frame");

            SlaveAddress = frame[0];
            FunctionCode = frame[1];
            ByteCount = frame[2];
            // _data = new RegisterCollection(frame.Slice(3, ByteCount).ToArray());
        }
    }
}
