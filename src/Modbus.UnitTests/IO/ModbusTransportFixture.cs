using System;
using System.IO;
using System.Linq;
using Modbus.Data;
using Modbus.IO;
using Modbus.Message;
using Modbus.Utility;
using NUnit.Framework;
using Rhino.Mocks;
using Modbus.Unme.Common;

namespace Modbus.UnitTests.IO
{
    delegate ReadCoilsInputsResponse ThrowExceptionDelegate();

    [TestFixture]
    public class ModbusTransportFixture
    {
        [Test]
        public void UnicastMessage()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();
            transport.Write(null);
            LastCall.IgnoreArguments();
            // read 4 coils from slave id 2
            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Return(new ReadCoilsInputsResponse(Modbus.ReadCoils, 2, 1, new DiscreteCollection(true, false, true, false, false, false, false, false)));

            Expect.Call(transport.OnValidateResponse(null, null))
                .IgnoreArguments()
                .Return(true);

            mocks.ReplayAll();

            var request = new ReadCoilsInputsRequest(Modbus.ReadCoils, 2, 3, 4);
            var expectedResponse = new ReadCoilsInputsResponse(Modbus.ReadCoils, 2, 1, new DiscreteCollection(true, false, true, false, false, false, false, false));
            var response = transport.UnicastMessage<ReadCoilsInputsResponse>(request);
            Assert.AreEqual(expectedResponse.MessageFrame, response.MessageFrame);

            mocks.VerifyAll();
        }

        [Test, ExpectedException(typeof(IOException))]
        public void UnicastMessage_WrongResponseFunctionCode()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();
            transport.Write(null);
            LastCall.IgnoreArguments().Repeat.Times(Modbus.DefaultRetries + 1);
            // read 4 coils from slave id 2
            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Return(new ReadCoilsInputsResponse(Modbus.ReadCoils, 2, 0, new DiscreteCollection()))
                .Repeat.Times(Modbus.DefaultRetries + 1);

            mocks.ReplayAll();

            var request = new ReadCoilsInputsRequest(Modbus.ReadInputs, 2, 3, 4);
            transport.UnicastMessage<ReadCoilsInputsResponse>(request);

            mocks.VerifyAll();
        }

        [Test, ExpectedException(typeof(SlaveException))]
        public void UnicastMessage_ErrorSlaveException()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();
            transport.Write(null);
            LastCall.IgnoreArguments().Repeat.Times(Modbus.DefaultRetries + 1);
            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Do((ThrowExceptionDelegate)delegate { throw new SlaveException(); });

            mocks.ReplayAll();

            var request = new ReadCoilsInputsRequest(Modbus.ReadInputs, 2, 3, 4);
            transport.UnicastMessage<ReadCoilsInputsResponse>(request);

            mocks.VerifyAll();
        }

        /// <summary>
        /// We should reread the response w/o retransmitting the request.
        /// </summary>
        [Test]
        public void UnicastMessage_AcknowlegeSlaveException()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();

            // set the wait to retry property to a small value so the test completes quickly
            transport.WaitToRetryMilliseconds = 5;

            transport.Write(null);
            LastCall.IgnoreArguments();

            // return a slave exception a greater number of times than number of retries to make sure we aren't just retrying
            Expect.Call(transport.ReadResponse<ReadHoldingInputRegistersResponse>())
                .Return(new SlaveExceptionResponse(1, Modbus.ReadHoldingRegisters + Modbus.ExceptionOffset, Modbus.Acknowledge))
                .Repeat.Times(transport.Retries + 1);

            Expect.Call(transport.ReadResponse<ReadHoldingInputRegistersResponse>())
                .Return(new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection(1)));

            transport.Stub(x => x.OnValidateResponse(null, null))
                .IgnoreArguments()
                .Return(true);

            mocks.ReplayAll();

            var request = new ReadHoldingInputRegistersRequest(Modbus.ReadHoldingRegisters, 1, 1, 1);
            var expectedResponse = new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection(1));
            var response = transport.UnicastMessage<ReadHoldingInputRegistersResponse>(request);
            Assert.AreEqual(expectedResponse.MessageFrame, response.MessageFrame);

            mocks.VerifyAll();
        }

        /// <summary>
        /// We should retransmit the request.
        /// </summary>
        [Test]
        public void UnicastMessage_SlaveDeviceBusySlaveException()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();

            // set the wait to retry property to a small value so the test completes quickly
            transport.WaitToRetryMilliseconds = 5;

            transport.Write(null);
            LastCall.IgnoreArguments()
                .Repeat.Times(2);

            // return a slave exception a greater number of times than number of retries to make sure we aren't just retrying
            Expect.Call(transport.ReadResponse<ReadHoldingInputRegistersResponse>())
                .Return(new SlaveExceptionResponse(1, Modbus.ReadHoldingRegisters + Modbus.ExceptionOffset, Modbus.SlaveDeviceBusy));

            Expect.Call(transport.ReadResponse<ReadHoldingInputRegistersResponse>())
                .Return(new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection(1)));

            Expect.Call(transport.OnValidateResponse(null, null))
                .IgnoreArguments()
                .Return(true);

            mocks.ReplayAll();

            var request = new ReadHoldingInputRegistersRequest(Modbus.ReadHoldingRegisters, 1, 1, 1);
            var expectedResponse = new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection(1));
            var response = transport.UnicastMessage<ReadHoldingInputRegistersResponse>(request);
            Assert.AreEqual(expectedResponse.MessageFrame, response.MessageFrame);

            mocks.VerifyAll();
        }

        /// <summary>
        /// We should retransmit the request.
        /// </summary>
        [Test]
        public void UnicastMessage_SlaveDeviceBusySlaveExceptionDoesNotFailAfterExceedingRetries()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();

            // set the wait to retry property to a small value so the test completes quickly
            transport.WaitToRetryMilliseconds = 5;

            transport.Write(null);
            LastCall.IgnoreArguments()
                .Repeat.Times(transport.Retries + 1);

            // return a slave exception a greater number of times than number of retries to make sure we aren't just retrying
            Expect.Call(transport.ReadResponse<ReadHoldingInputRegistersResponse>())
                .Return(new SlaveExceptionResponse(1, Modbus.ReadHoldingRegisters + Modbus.ExceptionOffset, Modbus.SlaveDeviceBusy))
                .Repeat.Times(transport.Retries);

            Expect.Call(transport.ReadResponse<ReadHoldingInputRegistersResponse>())
                .Return(new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection(1)));

            Expect.Call(transport.OnValidateResponse(null, null))
                .IgnoreArguments()
                .Return(true);

            mocks.ReplayAll();

            var request = new ReadHoldingInputRegistersRequest(Modbus.ReadHoldingRegisters, 1, 1, 1);
            var expectedResponse = new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection(1));
            var response = transport.UnicastMessage<ReadHoldingInputRegistersResponse>(request);
            Assert.AreEqual(expectedResponse.MessageFrame, response.MessageFrame);

            mocks.VerifyAll();
        }

        [TestCase(typeof(TimeoutException)),
        TestCase(typeof(IOException)),
        TestCase(typeof(NotImplementedException)),
        TestCase(typeof(FormatException))]
        public void UnicastMessage_SingleFailingException(Type exceptionType)
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();
            transport.Retries = 1;
            transport.Write(null);
            LastCall.IgnoreArguments().Repeat.Times(2);
            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Do((ThrowExceptionDelegate)delegate { throw (Exception)Activator.CreateInstance(exceptionType); });

            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Return(new ReadCoilsInputsResponse(Modbus.ReadCoils, 2, 1, new DiscreteCollection(true, false, true, false, false, false, false, false)));

            Expect.Call(transport.OnValidateResponse(null, null))
                .IgnoreArguments()
                .Return(true);

            mocks.ReplayAll();

            var request = new ReadCoilsInputsRequest(Modbus.ReadCoils, 2, 3, 4);
            transport.UnicastMessage<ReadCoilsInputsResponse>(request);

            mocks.VerifyAll();
        }

        [TestCase(typeof(TimeoutException)),
        TestCase(typeof(IOException)),
        TestCase(typeof(NotImplementedException)),
        TestCase(typeof(FormatException))]
        public void UnicastMessage_TooManyFailingExceptions(Type exceptionType)
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();

            transport.Write(null);
            LastCall.IgnoreArguments().Repeat.Times(transport.Retries + 1);

            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Do((ThrowExceptionDelegate)delegate { throw (Exception)Activator.CreateInstance(exceptionType); })
                .Repeat.Times(transport.Retries + 1);

            mocks.ReplayAll();

            var request = new ReadCoilsInputsRequest(Modbus.ReadCoils, 2, 3, 4);

            Assert.Throws(exceptionType, () => transport.UnicastMessage<ReadCoilsInputsResponse>(request));

            mocks.VerifyAll();
        }

        [Test, ExpectedException(typeof(TimeoutException))]
        public void UnicastMessage_TimeoutException()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();
            transport.Write(null);
            LastCall.IgnoreArguments().Repeat.Times(Modbus.DefaultRetries + 1);
            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Do((ThrowExceptionDelegate)delegate { throw new TimeoutException(); })
                .Repeat.Times(Modbus.DefaultRetries + 1);

            mocks.ReplayAll();

            var request = new ReadCoilsInputsRequest(Modbus.ReadInputs, 2, 3, 4);
            transport.UnicastMessage<ReadCoilsInputsResponse>(request);

            mocks.VerifyAll();
        }

        [Test, ExpectedException(typeof(TimeoutException))]
        public void UnicastMessage_Retries()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();
            transport.Retries = 5;
            transport.Write(null);
            LastCall.IgnoreArguments().Repeat.Times(transport.Retries + 1);
            Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
                .Do((ThrowExceptionDelegate)delegate { throw new TimeoutException(); })
                .Repeat.Times(transport.Retries + 1);

            mocks.ReplayAll();

            var request = new ReadCoilsInputsRequest(Modbus.ReadInputs, 2, 3, 4);
            transport.UnicastMessage<ReadCoilsInputsResponse>(request);

            mocks.VerifyAll();
        }

        [Test]
        public void CreateResponse_SlaveException()
        {
            ModbusTransport transport = new ModbusAsciiTransport(MockRepository.GenerateStub<IStreamResource>());
            byte[] frame = { 2, 129, 2 };
            IModbusMessage message = transport.CreateResponse<ReadCoilsInputsResponse>(frame.Concat(SequenceUtility.ToSequence(ModbusUtility.CalculateLrc(frame))).ToArray());
            Assert.IsTrue(message is SlaveExceptionResponse);
        }

        [Test]
        public void ValidateResponse_MismatchingFunctionCodes()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();

            IModbusMessage request = new ReadCoilsInputsRequest(Modbus.ReadCoils, 1, 1, 1);
            IModbusMessage response = new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection());

            mocks.ReplayAll();
            Assert.Throws<IOException>(() => transport.ValidateResponse(request, response));
        }

        [Test]
        public void ValidateResponse_CallsOnValidateResponse()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();

            IModbusMessage request = new ReadCoilsInputsRequest(Modbus.ReadCoils, 1, 1, 1);
            IModbusMessage response = new ReadCoilsInputsResponse(Modbus.ReadCoils, 1, 1, null);

            Expect.Call(transport.OnValidateResponse(request, response))
                .Return(true);

            mocks.ReplayAll();
            transport.ValidateResponse(request, response);
            mocks.VerifyAll();
        }

        [Test]
        public void UnicastMessage_ReReadsIfValidateResponseIsFalse()
        {
            var mocks = new MockRepository();
            var transport = mocks.PartialMock<ModbusTransport>();
            transport.WaitToRetryMilliseconds = 5;

            transport.Write(null);
            LastCall.IgnoreArguments();

            Expect.Call(transport.ReadResponse<ReadHoldingInputRegistersResponse>())
                .Return(new ReadHoldingInputRegistersResponse(Modbus.ReadHoldingRegisters, 1, new RegisterCollection(1)))
                .Repeat.Times(2)
                .Message("ReadResponse should be called twice, one for the retry");

            Expect.Call(transport.OnValidateResponse(null, null))
                .IgnoreArguments()
                .Return(false)
                .Repeat.Times(1);
            Expect.Call(transport.OnValidateResponse(null, null))
                .IgnoreArguments()
                .Return(true)
                .Repeat.Times(1);

            mocks.ReplayAll();
            var request = new ReadHoldingInputRegistersRequest(Modbus.ReadHoldingRegisters, 1, 1, 1);
            var response = transport.UnicastMessage<ReadHoldingInputRegistersResponse>(request);

            mocks.VerifyAll();

        }
    }
}