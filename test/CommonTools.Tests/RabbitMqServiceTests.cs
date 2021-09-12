using CommonTools.Interfaces;
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

namespace CommonTools.Tests
{
    public class RabbitMqServiceTests
    {
        private const string Queue = "testQueue";
        private string _exchange;
        private string _rRoutingKey;
        private string _sRoutingKey;
        private ManualResetEvent _waitHandle;
        private Mock<IConnectionFactory> _mockFactory;
        private Mock<IModel> _mockChannel;
        private IRabbitMqService _rabbitMqService;
        private int _receivedHandlerCalled;

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
                ++_receivedHandlerCalled;
                _waitHandle.Set();
            }

            var logger = new NullLogger<RabbitMqService>();

            _rabbitMqService = new RabbitMqService(
                _mockFactory.Object,
                properties.Settings,
                ReceiveHandler,
                logger);
        }

        [Test]
        public void RabbitMQ_Exchange_Is_Declared_Correctly_For_Receiving()
        {
            _waitHandle.Set();
            _rabbitMqService.StartReceiving();
            _mockChannel.Verify(ch => ch.ExchangeDeclare(_exchange, "topic", false, false, null), Times.Once);
        }

        [Test]
        public void RabbitMQ_Exchange_Is_Declared_Correctly_For_Sending()
        {
            _waitHandle.Set();
            _rabbitMqService.SendMessage("test message");
            _mockChannel.Verify(ch => ch.ExchangeDeclare(_exchange, "topic", false, false, null), Times.Once);
        }

        [Test]
        public void RabbitMQ_Channel_Is_Declared_Correctly_For_Receiving()
        {
            _waitHandle.Set();
            _rabbitMqService.StartReceiving();
            _mockChannel.Verify(ch => ch.QueueDeclare("", false, true, true, null), Times.Once);
        }

        [Test]
        public void RabbitMQ_Channel_QueueBind_Is_Done_Correctly_For_Receiving()
        {
            _waitHandle.Set();
            _rabbitMqService.StartReceiving();
            _mockChannel.Verify(ch => ch.QueueBind(Queue, _exchange, _rRoutingKey, null), Times.Once);
        }

        [Test]
        public void RabbitMQ_Uses_Correct_Channel_And_Queue_For_Receiving()
        {
            _waitHandle.Set();
            _rabbitMqService.StartReceiving();
            _mockChannel.Verify(
                ch => ch.BasicConsume(
                    Queue,
                    true,
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    null,
                    It.Is<IBasicConsumer>(bc => bc.Model == _mockChannel.Object)),
                Times.Once);
        }

        [Test]
        public void Call_ReceivedHandler_When_New_Delivery_Arrives()
        {
            _rabbitMqService.StartReceiving();

            var testBody = Encoding.UTF8.GetBytes("test message");
            
            _rabbitMqService.TriggerDeliveryReceived("testtag", 1201203, false, _exchange, _rRoutingKey, null, testBody);
            _waitHandle.WaitOne();
            
            Assert.AreEqual(1, _receivedHandlerCalled);
        }

        [Test]
        public void SendMessage_Sends_Correct_Message_To_Correct_Exchange_With_Correct_RoutingKey()
        {
            var testMessage = "test message lul";
            var expectedOutputBody = Encoding.UTF8.GetBytes(testMessage);
            _rabbitMqService.SendMessage(testMessage);
            _mockChannel.Verify(
                ch => ch.BasicPublish(
                    _exchange,
                    _sRoutingKey,
                    It.IsAny<bool>(),
                    null,
                    It.Is<ReadOnlyMemory<byte>>(b => b.ToArray().SequenceEqual(expectedOutputBody))),
                Times.Once);
        }
    }
}
