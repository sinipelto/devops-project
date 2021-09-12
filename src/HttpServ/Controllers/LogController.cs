using CommonTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HttpServ.Controllers
{
    [ApiController]
    [Route("{*url}")]
    public class LogController : ControllerBase
    {
        private readonly ILogger<LogController> _logger;
        private readonly string _logFile;

        public LogController(ILogger<LogController> logger, IOptions<ApplicationSettings> options)
        {
            _logger = logger;
            _logFile = options.Value.MsgLogFile;
        }

        [HttpGet]
        public string Get()
        {
            _logger.LogDebug("Reading log file..");
            return string.Join('\n', System.IO.File.ReadAllLines(_logFile));
        }
    }
}
