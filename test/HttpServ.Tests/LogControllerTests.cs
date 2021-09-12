using CommonTools.Models;
using HttpServ.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace HttpServ.Tests
{
    public class LogControllerTests
    {
        private LogController _logController;
        private const string LogFile = "Seeds/logfile.txt";

        [SetUp]
        public void Setup()
        {
            var logger = new NullLogger<LogController>();
            var options = Options.Create(new ApplicationSettings { MsgLogFile = LogFile });
            _logController = new LogController(logger, options);
        }

        [Test]
        public void Controller_Returns_LogFileContent_With_Linebreaks()
        {
            var expected = System.IO.File.ReadAllText(LogFile);
            var actual = _logController.Get();
            Assert.AreEqual(expected, actual);
        }
    }
}
