using ApiGateway.Controllers;
using ApiGateway.Interfaces;
using ApiGateway.Models;
using CommonTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiGateway.Tests
{
    public class ApiGatewayControllerTests
    {
        private GatewayController _controller;
        private Mock<IStateService> _stateServiceMock;
        private Mock<IRabbitMqManagementService> _rabbitMqManagementServiceMock;
        private Mock<ILogMessageService> _logMsgServiceMock;

        private static Mock<LogEntry> GenLogEntry(ServiceState msg)
        {
            var mock = new Mock<LogEntry>(MockBehavior.Strict, msg);
            mock.Object.Message = msg.ToString();
            mock.Object.TimeStamp = DateTime.Now;
            mock.Setup(i => i.ToString()).Returns(mock.Object.TimeStamp + ": " + mock.Object.Message);
            return mock;
        }

        [SetUp]
        public void Setup()
        {
            _stateServiceMock = new Mock<IStateService>(MockBehavior.Strict);
            _rabbitMqManagementServiceMock = new Mock<IRabbitMqManagementService>(MockBehavior.Strict);
            _logMsgServiceMock = new Mock<ILogMessageService>(MockBehavior.Strict);

            var logger = new NullLogger<GatewayController>();
            _controller = new GatewayController(logger, _stateServiceMock.Object, _rabbitMqManagementServiceMock.Object, _logMsgServiceMock.Object);
        }

        [Test]
        public async Task Test_GetRunLog_WithEmptyPayload()
        {
            _stateServiceMock.Setup(i => i.GetRunLogAsync()).ReturnsAsync(new List<LogEntry>()).Verifiable();

            Assert.AreEqual("", await _controller.GetRunLog());

            _stateServiceMock.Verify();
            _stateServiceMock.VerifyNoOtherCalls();
            _logMsgServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Test_ControllerGet_InitialState()
        {

            _stateServiceMock.Setup(i => i.GetCurrentStateAsync()).ReturnsAsync(ServiceState.INIT);
            _stateServiceMock.Setup(i => i.GetRunLogAsync()).ReturnsAsync(() =>
            {
                var entry = GenLogEntry(ServiceState.INIT);
                return new List<LogEntry> { entry.Object };
            });

            // No logs provided yet => service returns null
            // => controller should return empty string
            _logMsgServiceMock.Setup(i => i.GetLogMessagesFromHttpServAsync()).Returns(Task.FromResult<string>(null)).Verifiable();

            var expectedEntry = await _stateServiceMock.Object.GetRunLogAsync();
            
            // No Newline at the end -> only one entry
            var expLogStr = $"{expectedEntry.First().TimeStamp}: {ServiceState.INIT}";

            Assert.AreEqual(ServiceState.INIT.ToString(), await _controller.GetState());
            Assert.AreEqual("", await _controller.GetMessages());
            Assert.AreEqual(expLogStr, await _controller.GetRunLog());

            _stateServiceMock.Verify(i => i.GetCurrentStateAsync(), Times.Once);
            _stateServiceMock.Verify(i => i.GetRunLogAsync(), Times.Exactly(2));

            _stateServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Test_ControllerGet_StateAfterLaunch()
        {
            _stateServiceMock.Setup(i => i.GetCurrentStateAsync()).ReturnsAsync(ServiceState.RUNNING);
            _stateServiceMock.Setup(i => i.GetRunLogAsync()).ReturnsAsync(() =>
            {
                var initEntry = GenLogEntry(ServiceState.INIT);
                var pausedEntry = GenLogEntry(ServiceState.PAUSED);
                var runningEntry = GenLogEntry(ServiceState.RUNNING);
               
                return new List<LogEntry> { initEntry.Object, pausedEntry.Object, runningEntry.Object };
            });
            _logMsgServiceMock.Setup(i => i.GetLogMessagesFromHttpServAsync()).ReturnsAsync("");

            var entries = (await _stateServiceMock.Object.GetRunLogAsync()).ToList();

            var expLogStr = $"{entries[0].TimeStamp}: {ServiceState.INIT}\n" +
                            $"{entries[1].TimeStamp}: {ServiceState.PAUSED}\n" +
                            $"{entries[2].TimeStamp}: {ServiceState.RUNNING}";

            Assert.AreEqual(ServiceState.RUNNING.ToString(), await _controller.GetState());
            Assert.AreEqual("RUNNING", (await _controller.GetState()));

            Assert.AreEqual("", await _controller.GetMessages());
            Assert.AreEqual(expLogStr, await _controller.GetRunLog());

            _logMsgServiceMock.Verify(i => i.GetLogMessagesFromHttpServAsync(), Times.Once);
            _stateServiceMock.Verify(i => i.GetCurrentStateAsync(), Times.AtLeastOnce);
            _stateServiceMock.Verify(i => i.GetRunLogAsync(), Times.Exactly(2));

            _stateServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        [TestCase("INIT")]
        [TestCase("PAUSED")]
        [TestCase("RUNNING")]
        [TestCase("SHUTDOWN")]
        public async Task Test_Controller_TestChangeStateToValidState(string state)
        {
            // This conversion expected to always succeed => using parse which throws on failure
            var stateVal = Enum.Parse<ServiceState>(state);

            _stateServiceMock.Setup(i => i.GetCurrentStateAsync()).ReturnsAsync(ServiceState.INIT).Verifiable();
            _stateServiceMock.Setup(i => i.SetStateAsync(It.IsAny<ServiceState>())).Returns(Task.CompletedTask).Verifiable();

            Assert.AreEqual(ServiceState.INIT.ToString(), await _controller.GetState());
            Assert.AreEqual("INIT", (await _controller.GetState()).ToString());

            await _controller.PutState(state);

            // Expect no change if the value was same than before (no invocation done)
            if (stateVal == ServiceState.INIT)
            {
                _stateServiceMock.Verify(i => i.SetStateAsync(It.IsAny<ServiceState>()), Times.Never());
            }
            // If state changed, ensure state service was invoked.
            else
            {
                _stateServiceMock.Verify(i => i.SetStateAsync(stateVal), Times.Once());
            }
            
            _stateServiceMock.Verify(i => i.GetCurrentStateAsync(), Times.AtLeastOnce);
            _stateServiceMock.VerifyNoOtherCalls();;
        }

        [Test]
        public async Task Test_Controller_PutInvalidState()
        {
            _stateServiceMock.Setup(i => i.GetCurrentStateAsync()).ReturnsAsync(ServiceState.RUNNING);
            _stateServiceMock.Setup(i => i.SetStateAsync(It.IsAny<ServiceState>())).Returns(Task.CompletedTask).Verifiable();

            Assert.AreEqual(ServiceState.RUNNING.ToString(), await _controller.GetState());
            Assert.AreEqual("RUNNING", (await _controller.GetState()).ToString());

            Assert.IsInstanceOf<BadRequestResult>(await _controller.PutState("INVALID_STATE_VALUE"));

            _stateServiceMock.Verify(i => i.SetStateAsync(It.IsAny<ServiceState>()), Times.Never());
        }
    }
}