using ApiGateway.Interfaces;
using ApiGateway.Services;
using CommonTools.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiGateway.Tests
{
    public class StateServiceTests
    {
        private IStateService _service;
        private Mock<IDockerHostService> _dockerMock;

        [SetUp]
        public void Setup()
        {
            // Arrange
            _dockerMock = new Mock<IDockerHostService>(MockBehavior.Strict);

            _dockerMock.Setup(i => i.StopContainersAsync()).Returns(Task.FromResult(true));
            _dockerMock.Setup(i => i.RestartContainersAsync()).Returns(Task.FromResult(true));
            _dockerMock.Setup(i => i.ResumeContainersAsync()).Returns(Task.FromResult(true));
            _dockerMock.Setup(i => i.PauseContainersAsync()).Returns(Task.FromResult(true));
            
            _service = new StateService(_dockerMock.Object);
        }

        [Test]
        public async Task Test_CreateService_EnsureObjectState()
        {
            Assert.AreEqual(ServiceState.RUNNING, await _service.GetCurrentStateAsync());

            _dockerMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Test_SetSameStateAgain()
        {
            Assert.AreEqual(ServiceState.RUNNING, await _service.GetCurrentStateAsync());
            Assert.AreEqual(2, (await _service.GetRunLogAsync()).Count());

            await _service.SetStateAsync(ServiceState.RUNNING);

            // Already running, so state not changed and no logs generated
            Assert.AreEqual(ServiceState.RUNNING, await _service.GetCurrentStateAsync());
            Assert.AreEqual(2, (await _service.GetRunLogAsync()).Count());

            _dockerMock.VerifyNoOtherCalls();
        }

        [Test]
        [TestCase(ServiceState.INIT)]
        [TestCase(ServiceState.PAUSED)]
        [TestCase(ServiceState.SHUTDOWN)]
        // already RUNNING -> its not a NEW state
        public async Task Test_CreateService_SetNewState(ServiceState state)
        {
            Assert.AreEqual(ServiceState.RUNNING, await _service.GetCurrentStateAsync());
            Assert.AreEqual(2, (await _service.GetRunLogAsync()).Count());

            // Await until state is set.
            await _service.SetStateAsync(state);

            if (state == ServiceState.INIT)
            {
                Assert.AreEqual(ServiceState.RUNNING, await _service.GetCurrentStateAsync());
                Assert.AreEqual(4, (await _service.GetRunLogAsync()).Count());

            }
            else
            {
                Assert.AreEqual(state, await _service.GetCurrentStateAsync());
                Assert.AreEqual(3, (await _service.GetRunLogAsync()).Count());
            }

            switch (state)
            {
                case ServiceState.INIT:
                    _dockerMock.Verify(i => i.RestartContainersAsync(), Times.Once);
                    break;
                case ServiceState.PAUSED:
                    _dockerMock.Verify(i => i.PauseContainersAsync(), Times.Once);
                    break;
                case ServiceState.RUNNING:
                    // Successful change to RUNNING state processed in another test
                    throw new ArgumentException("This test should not be run with parameter RUNNING. It is not a NEW state.");
                case ServiceState.SHUTDOWN:
                    _dockerMock.Verify(i => i.StopContainersAsync(), Times.Once);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            _dockerMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Test_SetStateRunning_FromPausedState()
        {
            Assert.AreEqual(ServiceState.RUNNING, await _service.GetCurrentStateAsync());
            Assert.AreEqual(2, (await _service.GetRunLogAsync()).Count());

            // Await until state is set.
            await _service.SetStateAsync(ServiceState.PAUSED);
            _dockerMock.Verify(i => i.PauseContainersAsync(), Times.Once);

            Assert.AreEqual(ServiceState.PAUSED, await _service.GetCurrentStateAsync());
            Assert.AreEqual(3, (await _service.GetRunLogAsync()).Count());

        }

        [Test]
        public async Task Test_EnsureInitialServiceState()
        {
            // Act
            var cur = await _service.GetCurrentStateAsync();
            var log = (await _service.GetRunLogAsync()).ToList();

            // Assert
            Assert.AreEqual(ServiceState.RUNNING, cur);
            
            Assert.AreEqual(2, log.Count);
            Assert.AreEqual("INIT", log[0].Message);
            Assert.AreEqual("RUNNING", log[1].Message);

            Assert.LessOrEqual(log.First().TimeStamp, DateTime.UtcNow); // Was before or now
            Assert.Greater(log.First().TimeStamp, DateTime.UtcNow.AddSeconds(-2)); // Was not very long ago e.g. 2 secs

            _dockerMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Test_ChangeState_EnsureLogsCorrect()
        {
            // Act
            var cur = await _service.GetCurrentStateAsync();
            var log = (await _service.GetRunLogAsync()).ToList();

            // Assert
            Assert.AreEqual(ServiceState.RUNNING, cur);

            Assert.AreEqual(2, log.Count);
            Assert.AreEqual("INIT", log[0].Message);
            Assert.AreEqual("RUNNING", log[1].Message);

            Assert.LessOrEqual(log.First().TimeStamp, DateTime.UtcNow); // Was before or now
            Assert.Greater(log.Last().TimeStamp, DateTime.UtcNow.AddSeconds(-2)); // Was not very long ago e.g. 2 secs

            _dockerMock.VerifyNoOtherCalls();
        }
    }
}