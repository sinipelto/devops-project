using CommonTools.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace Observer
{
    public static class RabbitMqReceive
    {
        public static void ReceiveHandler(object model, BasicDeliverEventArgs ea, IModel channel, ApplicationSettings settings)
        {
            var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            Console.WriteLine("GOT MESSAGE");
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var queue = ea.RoutingKey;
            File.AppendAllText(settings.MsgLogFile, $"{timeStamp} Topic {queue}: {message}\n");
        }
    }
}
