using CommonTools.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

namespace Intermediate
{
    public static class RabbitMqReceive
    {
        public static void ReceiveHandler(object model, BasicDeliverEventArgs ea, IModel channel, ApplicationSettings settings)
        {
            Console.WriteLine("MESSAGE RECEIVED");
            Thread.Sleep(1000);
            
            var rBody = ea.Body.ToArray();
            var rMessage = Encoding.UTF8.GetString(rBody);
            var sMessage = $"Got {rMessage}";
            var sBody = Encoding.UTF8.GetBytes(sMessage);
            
            channel.BasicPublish(
                settings.RabbitMq.Exchange,
                settings.RabbitMq.SendRoutingKey,
                null,
                sBody);
        }
    }
}
