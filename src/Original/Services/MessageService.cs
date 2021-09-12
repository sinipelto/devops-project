using System.Threading;
using CommonTools.Interfaces;

namespace Original.Services
{
    public static class MessageService
    {
        /*
            Passing a negative number to numberOfMessages will send messages forever
        */
        public static void SendMessages(long numberOfMessages, int intervalInMilliseconds, IRabbitMqService rabbitMqService)
        {
            long messageNumber = 1;
            while (messageNumber != numberOfMessages + 1)
            {
                System.Console.WriteLine("Sending message");
                
                var message = $"MSG_{messageNumber}";
                
                rabbitMqService.SendMessage(message);
                
                ++messageNumber;
                Thread.Sleep(intervalInMilliseconds);
            }
        }
    }
}
