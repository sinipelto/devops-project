using CommonTools.Models;
using CommonTools.Services;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedTestUtils;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Observer.Tests
{
    public class Tests
    {
        private string _exchange;
        private const string Queue = "testQueue";
        private string _rRoutingKey;
        private string _msgLogFile;
        private ManualResetEvent _waitHandle;
        private Mock<IConnectionFactory> _mockFactory;
        private RabbitMqService _rabbitMqService;

        [SetUp]
        public void Setup()
        {
            var properties = RabbitMqTestUtils.SetUpMockRabbitMq(Queue);
            _msgLogFile = properties.Settings.MsgLogFile;
            _exchange = properties.Settings.RabbitMq.Exchange;
            _rRoutingKey = properties.Settings.RabbitMq.ReceiveRoutingKey;
            _waitHandle = properties.WaitHandle;
            _mockFactory = properties.MockFactory;

            // Clear test file content
            File.WriteAllText(_msgLogFile, "");

            void ReceiveHandler(object model, BasicDeliverEventArgs ea, IModel channel, ApplicationSettings settings)
            {
                RabbitMqReceive.ReceiveHandler(model, ea, channel, settings);
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
        public void RabbitMq_Messages_Are_Written_To_MsgLog_Correctly()
        {
            _rabbitMqService.StartReceiving();
            
            var inputString = "test message";
            var inputBody = Encoding.UTF8.GetBytes(inputString);

            var expectedOutputString = $"Topic {_rRoutingKey}: {inputString}";

            var startTime = DateTime.UtcNow;
            
            _rabbitMqService.TriggerDeliveryReceived("testtag", 1201203, false, _exchange, _rRoutingKey, null, inputBody);
            _waitHandle.WaitOne();
            var endTime = DateTime.UtcNow;
            var msgs = File.ReadAllText(_msgLogFile);
            var dateAndMessageString = msgs.Split(' ', 2);
            Assert.AreEqual(2, dateAndMessageString.Length, "Msg log is empty or otherwise not correct");
            var dateString = dateAndMessageString[0];
            var messageString = dateAndMessageString[1].Trim('\n');
            Assert.True(
                DateTime.TryParse(
                    dateString,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal,
                    out DateTime timeStamp),
                $"Parsing a date from message: {messageString} failed");
            Assert.True(startTime <= timeStamp && timeStamp <= endTime, "Timestamp of message is wrong");
            Assert.AreEqual(expectedOutputString, messageString, "Actual message is wrong");
        }
    }
}
