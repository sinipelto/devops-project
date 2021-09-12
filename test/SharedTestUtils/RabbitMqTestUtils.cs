using System;
using System.Threading;
using CommonTools.Models;
using CommonTools.Utils;
using Moq;
using RabbitMQ.Client;

namespace SharedTestUtils
{
    public static class RabbitMqTestUtils
    {
        public static RabbitMqTestProperties SetUpMockRabbitMq(string queue)
        {
            var settings = ConfigurationTools.ReadConfiguration<ApplicationSettings>("appsettings.Test.json");
            var exchange = settings.RabbitMq.Exchange;
            var rRoutingKey = settings.RabbitMq.ReceiveRoutingKey;
            var sRoutingKey = settings.RabbitMq.SendRoutingKey;

            var result = new QueueDeclareOk(queue, 0, 0);

            var mockChannel = new Mock<IModel>();
            
            mockChannel.Setup(ch => ch.ExchangeDeclare(exchange, "topic", false, false, null));
            mockChannel.Setup(ch => ch.QueueDeclare("", false, true, true, null)).Returns(result);
            mockChannel.Setup(ch => ch.QueueBind(queue, exchange, rRoutingKey, null));
            
            mockChannel.Setup(
                ch => ch.BasicConsume(
                    queue,
                    true,
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    null,
                    It.IsAny<IBasicConsumer>()));
            
            mockChannel.Setup(
                ch => ch.BasicPublish(
                    exchange,
                    sRoutingKey,
                    It.IsAny<bool>(),
                    null,
                    It.IsAny<ReadOnlyMemory<byte>>()));

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(conn => conn.CreateModel()).Returns(mockChannel.Object);

            var mockFactory = new Mock<IConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateConnection()).Returns(mockConnection.Object);

            return new RabbitMqTestProperties
            {
                MockChannel = mockChannel,
                MockConnection = mockConnection,
                MockFactory = mockFactory,
                Settings = settings,
                WaitHandle = new ManualResetEvent(false)
            };
        }
    }
}