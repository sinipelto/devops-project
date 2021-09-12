using ApiGateway.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ApiGateway.Tests
{
    public class LogEntryTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_GetTimestamp()
        {
            const string msg = "sumthin";
            var log = new LogEntry(msg);
            Assert.LessOrEqual(log.TimeStamp, DateTime.UtcNow);
            Assert.Greater(log.TimeStamp, DateTime.UtcNow.AddSeconds(-1));
            Assert.AreEqual(msg, log.Message);
        }

        [Test]
        public void Test_MultipleRecords()
        {
            const string msg = "notrelevant";

            var logs = new List<LogEntry>();
            
            for (var i = 0; i < 3; i++)
            {
                logs.Add(new LogEntry(msg));
                Thread.Sleep(1000);
            }

            Assert.Greater(logs[2].TimeStamp, logs[1].TimeStamp);
            Assert.Greater(logs[1].TimeStamp, logs[0].TimeStamp);
            logs.ForEach(log => Assert.LessOrEqual(log.TimeStamp, DateTime.UtcNow));
        }
    }
}