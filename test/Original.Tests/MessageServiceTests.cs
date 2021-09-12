using CommonTools.Interfaces;
using Moq;
using NUnit.Framework;
using Original.Services;
using System;
using System.Collections.Generic;

namespace Original.Tests
{
    public class Tests
    {
        private Mock<IRabbitMqService> _mockRabbitMqService;

        [SetUp]
        public void Setup()
        {
            _mockRabbitMqService = new Mock<IRabbitMqService>();
            _mockRabbitMqService.Setup(m => m.SendMessage(It.IsAny<string>()));
        }

        [Test]
        public void MessageService_Sends_Correct_Number_Of_Messages()
        {
            int numberOfMessages = 5;
            int intervalInMilliseconds = 10;
            MessageService.SendMessages(numberOfMessages, intervalInMilliseconds, _mockRabbitMqService.Object);
            _mockRabbitMqService.Verify(m => m.SendMessage(It.IsAny<string>()), Times.Exactly(numberOfMessages));
        }

        [Test]
        public void MessageService_Sends_Correct_Messages()
        {
            int numberOfMessages = 4;
            int intervalInMilliseconds = 10;
            var mockRabbitMqService = new Mock<IRabbitMqService>(MockBehavior.Strict);
            var seq = new MockSequence();
            mockRabbitMqService.InSequence(seq).Setup(m => m.SendMessage("MSG_1"));
            mockRabbitMqService.InSequence(seq).Setup(m => m.SendMessage("MSG_2"));
            mockRabbitMqService.InSequence(seq).Setup(m => m.SendMessage("MSG_3"));
            mockRabbitMqService.InSequence(seq).Setup(m => m.SendMessage("MSG_4"));
            Assert.DoesNotThrow(
                () => MessageService.SendMessages(
                    numberOfMessages,
                    intervalInMilliseconds,
                    mockRabbitMqService.Object),
                    "Messages were not sent in correct order");
            
        }

        [Test]
        public void MessageService_Sends_Messages_With_Correct_Intervals()
        {
            int numberOfMessages = 4;
            int intervalInMilliseconds = 500;
            int executionTimeOffset = 100;

            List<DateTime> timeStamps = new List<DateTime>();
            _mockRabbitMqService
                .Setup(m => m.SendMessage(It.IsAny<string>()))
                .Callback(() => timeStamps.Add(DateTime.Now));
            
            MessageService.SendMessages(
                numberOfMessages,
                intervalInMilliseconds,
                _mockRabbitMqService.Object);
            
            Assert.AreEqual(numberOfMessages, timeStamps.Count);
            DateTime? previous = null;
            timeStamps.ForEach(t => {
                if (previous != null)
                {
                    var ts = t - previous;
                    Assert.True(
                        ts?.TotalMilliseconds >= intervalInMilliseconds
                        && ts?.TotalMilliseconds < executionTimeOffset + intervalInMilliseconds,
                        "Interval was not followed");
                }
                previous = t;
            });
            
        }
    }
}