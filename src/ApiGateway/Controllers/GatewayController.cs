using ApiGateway.Interfaces;
using ApiGateway.Models;
using CommonTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("")]
    public class GatewayController : ControllerBase
    {
        private readonly ILogger<GatewayController> _logger;
        private readonly IStateService _stateService;
        private readonly IRabbitMqManagementService _rabbitMqManagementService;
        private readonly ILogMessageService _logMessageService;

        public GatewayController(
            ILogger<GatewayController> logger,
            IStateService stateService,
            IRabbitMqManagementService rabbitMqManagementService,
            ILogMessageService logMessageService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _rabbitMqManagementService = rabbitMqManagementService ?? throw new ArgumentNullException(nameof(rabbitMqManagementService));
            _logMessageService = logMessageService ?? throw new ArgumentNullException(nameof(logMessageService));
        }

        [HttpGet("messages")]
        public async Task<string> GetMessages() => await _logMessageService.GetLogMessagesFromHttpServAsync().ConfigureAwait(false) ?? "";

        [HttpPut("state")]
        public async Task<StatusCodeResult> PutState([FromBody] string state)
        {
            var result = Enum.TryParse<ServiceState>(state, out var parsed);

            // State input was not recognized.
            if (!result) return BadRequest();

            // No need to change the state, already active.
            if (parsed == await _stateService.GetCurrentStateAsync().ConfigureAwait(false)) 
                return Ok();

            _logger.LogInformation($"Setting state to {parsed}");
            await _stateService.SetStateAsync(parsed).ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("state")]
        public async Task<string> GetState() => (await _stateService.GetCurrentStateAsync().ConfigureAwait(false)).ToString();

        [HttpGet("run-log")]
        public async Task<string> GetRunLog()
        {
            _logger.LogInformation("Getting run log");
           var logs = await _stateService.GetRunLogAsync().ConfigureAwait(false);
           return string.Join("\n",logs.Select(i => i.ToString()));
        }

        [HttpGet("node-statistic")]
        public Task<List<RabbitMqNodesStats>> GetNodeStatistics()
        {
            _logger.LogInformation("Getting node stats");
            return _rabbitMqManagementService.GetNodesStatsAsync();
        }
    }
}
