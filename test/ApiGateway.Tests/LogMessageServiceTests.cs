using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiGateway.Services;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace ApiGateway.Tests
{
    public class LogMessageServiceTests
    {
        private LogMessageService _service;
        private Mock<HttpMessageHandler> _handlerMock;

        [SetUp]
        public void Setup()
        {
            // ARRANGE
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            
            var httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://mock.test"),
            };

            _service = new LogMessageService(httpClient);
        }

        [Test]
        public async Task Test_GetLogMessages_ActualAsExpected()
        {
            var expected = await File.ReadAllTextAsync("Expected/LogFileExpected.txt")
                .ConfigureAwait(false);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>( "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expected),
                })
                .Verifiable();

            var result = await _service.GetLogMessagesFromHttpServAsync();

            Assert.AreEqual(expected, result);
        }
    }
}