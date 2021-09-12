using ApiGateway.Interfaces;
using ApiGateway.Models;
using CommonTools.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiGateway.Services
{
    public class StateService : IStateService
    {
        private readonly IDockerHostService _dockerService;
        private readonly IList<LogEntry> _runLog;

        private ServiceState? _currentState;

        public StateService(IDockerHostService dockerService)
        {
            _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));

            // empty run log at startup
            _runLog = new List<LogEntry>();

            // Service started -> state is running
            _ = SetStateAsync(ServiceState.INIT, true);
        }

        public Task SetStateAsync(ServiceState state) => SetStateAsync(state, false);
        
        private async Task SetStateAsync(ServiceState state, bool initial)
        {
            // If state not changed, do nothing and return
            if (_currentState == state) return;

            if (_currentState != state)
            {
                var previousState = _currentState;
                _currentState = state;
                _runLog.Add(new LogEntry(state));

                switch (state)
                {
                    case ServiceState.PAUSED:
                        var result = await _dockerService.PauseContainersAsync()
                            .ConfigureAwait(false);
                        if (!result) throw new Exception("Could not pause containers.");
                        break;
                    case ServiceState.INIT:
                        // This was the first time to set the state (SINGLETON),
                        // so do Not execute a restart!
                        if (initial)
                        {
                            await SetStateAsync(ServiceState.RUNNING)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await _dockerService.RestartContainersAsync()
                                .ContinueWith(
                                    async retVal => await retVal.ConfigureAwait(false) 
                                        ? SetStateAsync(ServiceState.RUNNING) 
                                        : throw new Exception("Could not restart containers."))
                                .ConfigureAwait(false);
                        }
                        break;
                    case ServiceState.RUNNING:
                        // If previously state was init, no resuming needs to be taken in place (as already restarted or first launch in action)
                        if (previousState != ServiceState.INIT)
                        {
                            await _dockerService.ResumeContainersAsync()
                                .ConfigureAwait(false);
                        }
                        break;
                    case ServiceState.SHUTDOWN:
                        await _dockerService.StopContainersAsync()
                            .ConfigureAwait(false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        public Task<ServiceState?> GetCurrentStateAsync() => Task.FromResult(_currentState);

        public async Task<IEnumerable<LogEntry>> GetRunLogAsync() => _runLog;
    }
}
