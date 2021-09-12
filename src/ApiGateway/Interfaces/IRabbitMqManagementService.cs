using ApiGateway.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiGateway.Interfaces
{
    public interface IRabbitMqManagementService
    {
        Task<List<RabbitMqNodesStats>> GetNodesStatsAsync();
    }
}
