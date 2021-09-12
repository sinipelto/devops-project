using ApiGateway.Models;
using CommonTools.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiGateway.Interfaces
{
    public interface IStateService
    {
        /// <summary>
        /// Set the state of the application complex.
        /// This service orchestrates the docker complex by using application states.
        /// </summary>
        /// <param name="state">The future state to be applied. An existing state will be ignored.</param>
        public Task SetStateAsync(ServiceState state);

        public Task<ServiceState?> GetCurrentStateAsync();

        /// <summary>
        /// Get information about state changes
        /// Example output:
        /// 2020-11-01T06:35:01.373Z: INIT
        /// 2020-11-01T06:40:01.373Z: PAUSED
        /// 2020-11-01T06:40:01.373Z: RUNNING
        /// </summary>
        /// <returns>List of run log entries as list of strings</returns>
        public Task<IEnumerable<LogEntry>> GetRunLogAsync();
    }
}