using ApiGateway.Services;
using CommonTools.Utils;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiGateway.Tests
{
    public class RabbitMqManagementServiceTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Handle_RabbitMq_Management_Response_Correctly()
        {
            var testData = await File.ReadAllTextAsync("Seeds/RabbitMqManagementNodesStatsSampleResponse.json");
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // prepare the expected response of the mocked http call
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(testData),
                });
            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com")
            };
            var logger = new NullLogger<RabbitMqManagementService>();
            var service = new RabbitMqManagementService(client, logger);
            var result = await service.GetNodesStatsAsync();

            var resultString = result.Serialize();
            var expected = await File.ReadAllTextAsync("Expected/RabbitMqNodesStatsExpected.json");

            Assert.AreEqual(expected, resultString);
        }
    }
}