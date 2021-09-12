using CommonTools.Models;
using CommonTools.Services;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedTestUtils;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Intermediate.Tests
{
    public class RabbitMqTests
    {
        private string _exchange;
        private const string Queue = "testQueue";
        private string _rRoutingKey;
        private string _sRoutingKey;
        private ManualResetEvent _waitHandle;
        private Mock<IConnectionFactory> _mockFactory;
        private Mock<IModel> _mockChannel;
        private RabbitMqService _rabbitMqService;

        [SetUp]
        public void Setup()
        {
            var properties = RabbitMqTestUtils.SetUpMockRabbitMq(Queue);
            
            _exchange = properties.Settings.RabbitMq.Exchange;
            _rRoutingKey = properties.Settings.RabbitMq.ReceiveRoutingKey;
            _sRoutingKey = properties.Settings.RabbitMq.SendRoutingKey;
            _waitHandle = properties.WaitHandle;
            _mockFactory = properties.MockFactory;
            _mockChannel = properties.MockChannel;

            void ReceiveHandler(object model, BasicDeliverEventArgs ea, IModel channel, ApplicationSettings settings)
            {
                RabbitMqReceive.ReceiveHandler(model, ea, channel, settings);
                _waitHandle.Set();
            }
            var logger = new NullLogger<RabbitMqService>();
            _rabbitMqService = new RabbitMqService(_mockFactory.Object, properties.Settings, ReceiveHandler, logger);
        }

        [Test]
        public void Handle_RabbitMQ_Messages_Correctly()
        {
            _rabbitMqService.StartReceiving();
            
            var inputBody = Encoding.UTF8.GetBytes("test message");
            var expectedOutputBody = Encoding.UTF8.GetBytes("Got test message");
            
            _rabbitMqService.TriggerDeliveryReceived("testtag", 1201203, false, _exchange, _rRoutingKey, null, inputBody);
            _waitHandle.WaitOne();
            _mockChannel.Verify(
                ch => ch.BasicPublish(
                    _exchange,
                    _sRoutingKey,
                    It.IsAny<bool>(),
                    null,
                    It.Is<ReadOnlyMemory<byte>>(b => b.ToArray().SequenceEqual(expectedOutputBody))));
        }
    }
}
