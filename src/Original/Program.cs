using CommonTools.Interfaces;
using CommonTools.Models;
using CommonTools.Services;
using CommonTools.Utils;
using Original.Services;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;

namespace Original
{
    internal class Program
    {
        private const int InitTimeout = 3000;
        private const int MessageInterval = 3000;

        public static void Main(string[] args)
        {
            var config = ConfigurationTools.ReadConfiguration<ApplicationSettings>("appsettings.json");
            var configuration = ConfigurationTools.GetConfiguration("appsettings.json");
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSeq(configuration.GetSection("Seq"));
            var rabbitLogger = loggerFactory.CreateLogger<IRabbitMqService>();
            bool.TryParse(Environment.GetEnvironmentVariable("TEST_RUN"), out bool testRun);

            // Make it more likely that other services are already up and running
            // We could also declare the queues here but that would prevent us from making the queues exclusive
            // Also declaring the queues here would increase the amount of configuration
            
            Thread.Sleep(InitTimeout);

            IRabbitMqService rabbitMqService = new RabbitMqService(config, rabbitLogger);
            MessageService.SendMessages(testRun ? 3 : -1, MessageInterval, rabbitMqService);
        }
    }
}
