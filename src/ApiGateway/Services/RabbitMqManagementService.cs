using ApiGateway.Interfaces;
using ApiGateway.Models;
using CommonTools.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Services
{
    public class RabbitMqManagementService : IRabbitMqManagementService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        public RabbitMqManagementService(HttpClient client, ILogger<IRabbitMqManagementService> logger)
        {
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<RabbitMqNodesStats>> GetNodesStatsAsync()
        {
            _logger.LogInformation("Getting node stats");
            var res = await _httpClient.GetAsync("/api/nodes").ConfigureAwait(false);
            
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError($"Getting node stats failed with status code: ${res.StatusCode}");
                return null;
            }
            
            var resString = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            var opts = JsonSerializerExtensions.DefaultOptions;
            opts.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
            return resString.Deserialize<List<RabbitMqNodesStats>>(opts);
        }
    }
}
