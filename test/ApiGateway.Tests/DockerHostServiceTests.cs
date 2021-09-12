using ApiGateway.Interfaces;
using ApiGateway.Services;
using CommonTools.Utils;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApiGateway.Tests
{
    public class DockerHostServiceTests
    {
        private Mock<IDockerClient> _mockDockerClient;
        private DockerHostService _dockerHostService;
        private Mock<IContainerOperations> _mockContainerOps;
        private string _mainImageName;
        private uint _mainImageKillTimeout;
        private readonly string _testId = "TESTID";
        private readonly string _testImageName = "TESTNAME";
        private readonly string _originalId = "ORIGINALID";
        private readonly string _originalImageName = "original_1";
        private readonly string _mainImageId = "MAIN";

        [SetUp]
        public void Setup()
        {
            var nullLogger = new NullLogger<IDockerHostService>();
            _mockDockerClient = new Mock<IDockerClient>(MockBehavior.Strict);
            _mockContainerOps = new Mock<IContainerOperations>(MockBehavior.Strict);
            var configuration = ConfigurationTools.GetConfiguration("appsettings.Test.json");
            _mainImageName = configuration.GetValue<string>("DockerApi:MainContainerName");
            _mainImageKillTimeout = configuration.GetValue<uint>("DockerApi:MainContainerStopTimeout");

            _mockContainerOps.Setup(o => o.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContainerListResponse>() {
                    new ContainerListResponse
                    {
                        Image = _testImageName,
                        ID = _testId
                    },
                    new ContainerListResponse
                    {
                        Image = _originalImageName,
                        ID = _originalId
                    },
                    new ContainerListResponse
                    {
                        Image = _mainImageName,
                        ID = _mainImageId
                    }
                });

            _mockDockerClient.Setup(d => d.Containers).Returns(_mockContainerOps.Object);

            _dockerHostService = new DockerHostService(nullLogger, configuration, _mockDockerClient.Object);
        }

        [Test]
        public void Test_Stopping_All_Containers()
        {
            var seq = new MockSequence();
            _mockContainerOps.InSequence(seq).Setup(
                o => o.StopContainerAsync(
                    _testId,
                    It.Is<ContainerStopParameters>(ctp => ctp.WaitBeforeKillSeconds == 0),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            _mockContainerOps.InSequence(seq).Setup(
                o => o.StopContainerAsync(
                    _originalId,
                    It.Is<ContainerStopParameters>(ctp => ctp.WaitBeforeKillSeconds == 0),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            _mockContainerOps.InSequence(seq).Setup(
                o => o.StopContainerAsync(
                    _mainImageId,
                    It.Is<ContainerStopParameters>(ctp => ctp.WaitBeforeKillSeconds == _mainImageKillTimeout),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            Assert.DoesNotThrowAsync(async () => 
            {
                Assert.IsTrue(
                    await _dockerHostService.StopContainersAsync(),
                    "Stopping containers did not work");
            }, "Containers were not stopped in the right order");
        }

        [Test]
        public async Task Test_RestartContainers()
        {
            var image1 = "SOMETRING_OBSERVER_1";
            var id1 = "ID1";

            var image2 = "OTHERSTRING_ORIGINAL_2";
            var id2 = "ID2";

            var image3 = "SOMEOTHER_CONTAINER_3";
            var id3 = "ID3";

            var image4 = "AGAIN_ANOTHER_CONTAINER_4";
            var id4 = "ID4";

            _mockContainerOps.Setup(i =>
                    i.ListContainersAsync(It.Is<ContainersListParameters>(comparer => comparer.All == true), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContainerListResponse>
                {
                    new ContainerListResponse
                    {
                        ID = id2,
                        Image = image2

                    },
                    new ContainerListResponse
                    {
                        ID = id1,
                        Image = image1
                    },
                    new ContainerListResponse
                    {
                        ID = id4,
                        Image = image4
                    },
                    new ContainerListResponse
                    {
                        ID = id3,
                        Image = image3
                    }
                });

            var seq = new MockSequence();

            _mockContainerOps.InSequence(seq).Setup(
                    o => o.RestartContainerAsync(
                        id1,
                        It.IsNotNull<ContainerRestartParameters>(),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockContainerOps.InSequence(seq).Setup(
                    o => o.RestartContainerAsync(
                        id2,
                        It.IsNotNull<ContainerRestartParameters>(),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockDockerClient.Setup(d => d.Containers).Returns(_mockContainerOps.Object);

            await _dockerHostService.RestartContainersAsync();

            _mockContainerOps.Verify(i =>
                i.RestartContainerAsync(
                    id1,
                    It.IsNotNull<ContainerRestartParameters>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockContainerOps.Verify(i =>
                    i.RestartContainerAsync(
                        id2,
                        It.IsNotNull<ContainerRestartParameters>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Test_Pausing_Original()
        {
            _mockContainerOps
                .Setup(o => o.PauseContainerAsync(_originalId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var res = await _dockerHostService.PauseContainersAsync();
            Assert.IsTrue(res);
        }

        [Test]
        public async Task Test_Resuming_Original()
        {
            _mockContainerOps
                .Setup(o => o.UnpauseContainerAsync(_originalId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var res = await _dockerHostService.ResumeContainersAsync();
            Assert.IsTrue(res);
        }
    }
}