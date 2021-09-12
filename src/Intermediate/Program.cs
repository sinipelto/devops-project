using CommonTools.Interfaces;
using CommonTools.Models;
using CommonTools.Services;
using CommonTools.Utils;
using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Intermediate
{
    internal class Program
    {
        private static readonly ManualResetEvent WaitHandle = new ManualResetEvent(false);

        public static void Main()
        {
            var settings = ConfigurationTools.ReadConfiguration<ApplicationSettings>("appsettings.json");
            var configuration = ConfigurationTools.GetConfiguration("appsettings.json");
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSeq(configuration.GetSection("Seq"));
            var rabbitLogger = loggerFactory.CreateLogger<IRabbitMqService>();
            IRabbitMqService rabbitMqService = new RabbitMqService(settings, RabbitMqReceive.ReceiveHandler, rabbitLogger);

            rabbitMqService.StartReceiving();

            // Prevent the program from exiting. This has to be done this way to be able to run it in docker.
            Console.CancelKeyPress += (o, e) =>
            {
                // Allow the main thread to continue execution
                WaitHandle.Set();
            };

            WaitHandle.WaitOne();
        }
    }
}
