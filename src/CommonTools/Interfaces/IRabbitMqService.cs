using RabbitMQ.Client;
using System;

namespace CommonTools.Interfaces
{
    public interface IRabbitMqService
    {
        void StartReceiving();
        void SendMessage(string msg);

        void TriggerDeliveryReceived(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body
            );
    }
}
