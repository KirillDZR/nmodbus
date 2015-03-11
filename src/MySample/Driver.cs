using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FtdAdapter;
using Modbus.Data;
using Modbus.Device;
using Modbus.Message;
using Modbus.Utility;

namespace MySample
{
    /// <summary>
    /// Demonstration of NModbus
    /// </summary>
    public class Driver
    {
        private static void Main(string[] args)
        {
            try
            {
                //ModbusTcpMasterReadInputs();
                //SimplePerfTest();
                //ModbusSerialRtuMasterWriteRegisters();
                //ModbusSerialAsciiMasterReadRegisters();
                //ModbusTcpMasterReadInputs();			
                //ModbusTcpMasterReadInputsFromModbusSlave();
                //ModbusSerialAsciiMasterReadRegistersFromModbusSlave();
                //StartModbusTcpSlave();
                //StartModbusUdpSlave();	
                //StartModbusSerialAsciiSlave();
                StartModbusSerialRtuSlave();
                //StartModbusTcpSlaveFileRead();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }

        /// <summary>
        /// Simple Modbus serial RTU master write holding registers example.
        /// </summary>
        public static void ModbusSerialRtuMasterWriteRegisters()
        {
            using (var port = new SerialPort("COM4"))
            {
                // configure serial port
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.Open();

                // create modbus master
                IModbusSerialMaster master = ModbusSerialMaster.CreateRtu(port);

                const byte slaveId = 1;
                const ushort startAddress = 100;
                var registers = new ushort[] {1, 2, 3};

                // write three registers
                master.WriteMultipleRegisters(slaveId, startAddress, registers);
            }
        }

        /// <summary>
        /// Simple Modbus serial ASCII master read holding registers example.
        /// </summary>
        public static void ModbusSerialAsciiMasterReadRegisters()
        {
            using (var port = new SerialPort("COM4"))
            {
                // configure serial port
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.Open();

                // create modbus master
                IModbusSerialMaster master = ModbusSerialMaster.CreateAscii(port);

                const byte slaveId = 1;
                const ushort startAddress = 1;
                const ushort numRegisters = 5;

                // read five registers		
                var registers = master.ReadHoldingRegisters(slaveId, startAddress, numRegisters);

                for (var i = 0; i < numRegisters; i++)
                    Console.WriteLine(@"Register {0}={1}", startAddress + i, registers[i]);
            }

            // output: 
            // Register 1=0
            // Register 2=0
            // Register 3=0
            // Register 4=0
            // Register 5=0
        }

        /// <summary>
        /// Simple Modbus serial USB RTU master write multiple coils example.
        /// </summary>
        public static void ModbusSerialUsbRtuMasterWriteCoils()
        {
            using (var port = new FtdUsbPort())
            {
                // configure usb port
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = FtdParity.None;
                port.StopBits = FtdStopBits.One;
                port.OpenByIndex(0);

                // create modbus master
                IModbusSerialMaster master = ModbusSerialMaster.CreateRtu(port);

                const byte slaveId = 1;
                const ushort startAddress = 1;

                // write three coils
                master.WriteMultipleCoils(slaveId, startAddress, new[] {true, false, true});
            }
        }

        /// <summary>
        /// Simple Modbus serial USB ASCII master write multiple coils example.
        /// </summary>
        public static void ModbusSerialUsbAsciiMasterWriteCoils()
        {
            using (var port = new FtdUsbPort())
            {
                // configure usb port
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = FtdParity.None;
                port.StopBits = FtdStopBits.One;
                port.OpenByIndex(0);

                // create modbus master
                IModbusSerialMaster master = ModbusSerialMaster.CreateAscii(port);

                const byte slaveId = 1;
                const ushort startAddress = 1;

                // write three coils
                master.WriteMultipleCoils(slaveId, startAddress, new[] {true, false, true});
            }
        }

        /// <summary>
        /// Simple Modbus TCP master read inputs example.
        /// </summary>
        public static void ModbusTcpMasterReadInputs()
        {
            using (var client = new TcpClient("127.0.0.1", 502))
            {
                var master = ModbusIpMaster.CreateIp(client);

                // read five input values
                const ushort startAddress = 100;
                const ushort numInputs = 5;
                var inputs = master.ReadInputs(startAddress, numInputs);

                for (var i = 0; i < numInputs; i++)
                    Console.WriteLine(@"Input {0}={1}", startAddress + i, inputs[i] ? 1 : 0);
            }

            // output: 
            // Input 100=0
            // Input 101=0
            // Input 102=0
            // Input 103=0
            // Input 104=0
        }

        /// <summary>
        /// Simple Modbus UDP master write coils example.
        /// </summary>
        public static void ModbusUdpMasterWriteCoils()
        {
            using (var client = new UdpClient())
            {
                var endPoint = new IPEndPoint(new IPAddress(new byte[] {127, 0, 0, 1}), 502);
                client.Connect(endPoint);

                var master = ModbusIpMaster.CreateIp(client);

                const ushort startAddress = 1;

                // write three coils
                master.WriteMultipleCoils(startAddress, new[] {true, false, true});
            }
        }

        /// <summary>
        /// Simple Modbus serial ASCII slave example.
        /// </summary>
        public static void StartModbusSerialAsciiSlave()
        {
            using (var slavePort = new SerialPort("COM3"))
            {
                // configure serial port
                slavePort.BaudRate = 9600;
                slavePort.DataBits = 8;
                slavePort.Parity = Parity.None;
                slavePort.StopBits = StopBits.One;
                slavePort.Open();

                const byte unitId = 1;

                // create modbus slave
                ModbusSlave slave = ModbusSerialSlave.CreateAscii(unitId, slavePort);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.Listen();
            }
        }

        /// <summary>
        /// Simple Modbus serial RTU slave example.
        /// </summary>
        public static void StartModbusSerialRtuSlave()
        {
            using (var slavePort = new SerialPort("COM1"))
            {
                // configure serial port
                slavePort.BaudRate = 9600;
                slavePort.DataBits = 8;
                slavePort.Parity = Parity.None;
                slavePort.StopBits = StopBits.One;
                slavePort.Open();

                const byte unitId = 1;

                // create modbus slave
                ModbusSlave slave = ModbusSerialSlave.CreateRtu(unitId, slavePort);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.Listen();
            }
        }

        /// <summary>
        /// Simple Modbus serial USB ASCII slave example.
        /// </summary>
        public static void StartModbusSerialUsbAsciiSlave()
        {
            // 
        }

        /// <summary>
        /// Simple Modbus serial USB RTU slave example.
        /// </summary>
        public static void StartModbusSerialUsbRtuSlave()
        {
            // 
        }

        /// <summary>
        /// Simple Modbus TCP slave example.
        /// </summary>
        public static void StartModbusTcpSlave()
        {
            try
            {
                const byte slaveId = 1;
                const int port = 502;
                var address = new IPAddress(new byte[] {127, 0, 0, 1});

                // create and start the TCP slave
                var slaveTcpListener = new TcpListener(address, port);
                slaveTcpListener.Start();

                ModbusSlave slave = ModbusTcpSlave.CreateTcp(slaveId, slaveTcpListener);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.Listen();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            // prevent the main thread from exiting
//            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// Simple Modbus UDP slave example.
        /// </summary>
        public static void StartModbusUdpSlave()
        {
            using (var client = new UdpClient(502))
            {
                var slave = ModbusUdpSlave.CreateUdp(client);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.Listen();

                // prevent the main thread from exiting
                Thread.Sleep(Timeout.Infinite);
            }
        }

        /// <summary>
        /// Modbus TCP master and slave example.
        /// </summary>
        public static void ModbusTcpMasterReadInputsFromModbusSlave()
        {
            const byte slaveId = 1;
            const int port = 502;
            var address = new IPAddress(new byte[] {127, 0, 0, 1});

            // create and start the TCP slave
            var slaveTcpListener = new TcpListener(address, port);
            slaveTcpListener.Start();
            ModbusSlave slave = ModbusTcpSlave.CreateTcp(slaveId, slaveTcpListener);
            var slaveThread = new Thread(slave.Listen);
            slaveThread.Start();

            // create the master
            var masterTcpClient = new TcpClient(address.ToString(), port);
            var master = ModbusIpMaster.CreateIp(masterTcpClient);

            const ushort numInputs = 5;
            const ushort startAddress = 100;

            // read five register values
            var inputs = master.ReadInputRegisters(startAddress, numInputs);

            for (var i = 0; i < numInputs; i++)
                Console.WriteLine(@"Register {0}={1}", startAddress + i, inputs[i]);

            // clean up
            masterTcpClient.Close();
            slaveTcpListener.Stop();

            // output
            // Register 100=0
            // Register 101=0
            // Register 102=0
            // Register 103=0
            // Register 104=0
        }

        /// <summary>
        /// Modbus serial ASCII master and slave example.
        /// </summary>
        public static void ModbusSerialAsciiMasterReadRegistersFromModbusSlave()
        {
            using (var masterPort = new SerialPort("COM4"))
            using (var slavePort = new SerialPort("COM3"))
            {
                // configure serial ports
                masterPort.BaudRate = slavePort.BaudRate = 9600;
                masterPort.DataBits = slavePort.DataBits = 8;
                masterPort.Parity = slavePort.Parity = Parity.None;
                masterPort.StopBits = slavePort.StopBits = StopBits.One;
                masterPort.Open();
                slavePort.Open();

                // create modbus slave on seperate thread
                const byte slaveId = 1;
                ModbusSlave slave = ModbusSerialSlave.CreateAscii(slaveId, slavePort);
                var slaveThread = new Thread(slave.Listen);
                slaveThread.Start();

                // create modbus master
                var master = ModbusSerialMaster.CreateAscii(masterPort);

                master.Transport.Retries = 5;
                const ushort startAddress = 100;
                const ushort numRegisters = 5;

                // read five register values
                var registers = master.ReadHoldingRegisters(slaveId, startAddress, numRegisters);

                for (var i = 0; i < numRegisters; i++)
                    Console.WriteLine(@"Register {0}={1}", startAddress + i, registers[i]);
            }

            // output
            // Register 100=0
            // Register 101=0
            // Register 102=0
            // Register 103=0
            // Register 104=0
        }

        /// <summary>
        /// Write a 32 bit value.
        /// </summary>
        public static void ReadWrite32BitValue()
        {
            using (var port = new SerialPort("COM1"))
            {
                // configure serial port
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.Open();

                // create modbus master
                var master = ModbusSerialMaster.CreateRtu(port);

                const byte slaveId = 1;
                const ushort startAddress = 1008;
                const uint largeValue = UInt16.MaxValue + 5;

                var lowOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(largeValue), 0);
                var highOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(largeValue), 2);

                // write large value in two 16 bit chunks
                master.WriteMultipleRegisters(slaveId, startAddress, new[] {lowOrderValue, highOrderValue});

                // read large value in two 16 bit chunks and perform conversion
                var registers = master.ReadHoldingRegisters(slaveId, startAddress, 2);
                var value = ModbusUtility.GetUInt32(registers[1], registers[0]);

                Console.WriteLine(value);
            }
        }

        /// <summary>
        /// Simple Modbus TCP slave example.
        /// </summary>
        public static void StartModbusTcpSlaveFileRead()
        {
            try
            {
                const byte slaveId = 1;
                /*
                const int port = 502;
                var address = new IPAddress(new byte[] {127, 0, 0, 1});

                // create and start the TCP slave
                var slaveTcpListener = new TcpListener(address, port);
                slaveTcpListener.Start();

                ModbusSlave slave = ModbusTcpSlave.CreateTcp(slaveId, slaveTcpListener);
                */

                var slavePort = new SerialPort("COM1")
                {
                    BaudRate = 38400,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                };
                slavePort.Open();

                ModbusSlave slave = ModbusSerialSlave.CreateRtu(slaveId, slavePort);

                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.RegisterCustomFunction<ReadFileRequest>(Modbus.Modbus.ReadFiles,
                    (request, dataStore) =>
                    {
                        if (request.FunctionCode != Modbus.Modbus.ReadFiles)
                        {
                            return new SlaveExceptionResponse(request.SlaveAddress, 0x94, 0x01);
                        }

                        if (request.ByteCount < 0x07 || request.ByteCount > 0xF5)
                        {
                            return new SlaveExceptionResponse(request.SlaveAddress, 0x94, 0x03);
                        }

                        var response = new ReadFileResponse
                        {
                            SlaveAddress = request.SlaveAddress
                        };

                        try
                        {
                            using (var file = new FileStream(@"..\..\..\..\README.txt", FileMode.Open))
                            {
                                foreach (var record in request.Data)
                                {
                                    var buffer = new byte[record.RecordLength * 2];
                                    file.Position = record.RecordNumber * buffer.Length;
                                    var count = file.Read(buffer, 0, buffer.Length);
                                    response.AddRecordData(buffer, count);
                                }
                            }
                        }
                        catch (IOException)
                        {
                            return new SlaveExceptionResponse(request.SlaveAddress, 0x94, 0x02);
                        }

                        return response;
                    });

                slave.Listen();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
