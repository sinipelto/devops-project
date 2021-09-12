using ApiGateway.Interfaces;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiGateway.Services
{
    public class DockerHostService : IDockerHostService
    {
        private readonly ILogger<IDockerHostService> _logger;
        private readonly IDockerClient _dockerClient;

        private readonly string _mainContainerName;
        private readonly uint _mainContainerStopTimeout;

        public DockerHostService(ILogger<IDockerHostService> logger, IConfiguration configuration, IDockerClient dockerClient)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));

            // Collect container name prefix from configuration as lowercase
            _mainContainerName = configuration.GetValue<string>("DockerApi:MainContainerName").ToLower();
            _mainContainerStopTimeout = configuration.GetValue<uint>("DockerApi:MainContainerStopTimeout");

            _logger.LogInformation($"Using main container name: {_mainContainerName}");
        }

        /// <summary>
        /// Pauses container ORIGINAL to stop generating more messages
        /// </summary>
        /// <returns></returns>
        public async Task<bool> PauseContainersAsync()
        {
            const string contStr = "original";

            // Throws exception if container for expected service not found
            var cont =
                (await _dockerClient.Containers
                    .ListContainersAsync(new ContainersListParameters { All = true })
                    .ConfigureAwait(false))
                .First(i => i.Image.Contains(contStr));

            await _dockerClient.Containers
                .PauseContainerAsync(cont.ID)
                .ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Stops all containers, including the container running this code.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StopContainersAsync()
        {
            var containers =
                (await _dockerClient.Containers
                    .ListContainersAsync(new ContainersListParameters {All = true})
                    .ConfigureAwait(false))
                .ToList();

            if (containers.Count <= 0)
            {
                _logger.LogError($"Could not find any containers.");
                return false;
            }

            ContainerListResponse thisContainer = null;

            foreach (var ctr in containers)
            {
                // Do NOT stop THIS container yet!
                // But collect it for later use
                if (ctr.Image.ToLower().Contains(_mainContainerName))
                {
                    _logger.LogInformation($"Found MAIN container: ID: {ctr.ID} Image: {ctr.Image}");
                    thisContainer = ctr;
                    continue;
                }

                var result = await _dockerClient.Containers
                    .StopContainerAsync(ctr.ID, new ContainerStopParameters {WaitBeforeKillSeconds = 0})
                    .ConfigureAwait(false);

                _logger.LogInformation($"Container {ctr.Image} stopped.");

                // All containers must be successfully stopped first
                if (!result) return false;
            }

            if (thisContainer == null) throw new ArgumentNullException(nameof(thisContainer), "Could not locate MAIN container.");

            // Wait for a while (5 secs) before stopping this container until the code has been able to pass through
            // Also run in background and return from the function immediately.
            _ = _dockerClient.Containers.StopContainerAsync(thisContainer.ID,
                new ContainerStopParameters {WaitBeforeKillSeconds = _mainContainerStopTimeout})
                .ContinueWith(async res =>
                {
                    if (!await res)
                    {
                        _logger.LogCritical("Could not stop MAIN container!");
                        throw new Exception($"Could not stop container: {thisContainer.ID} -- {thisContainer.Image}");
                    }
                });

            return true;
        }

        /// <summary>
        /// Restarts OBSERVER and ORIGINAL containers
        /// Clears message history and starts message counting from beginning.
        /// </summary>
        /// <returns>Boolean value of operation succeeding or not</returns>
        public async Task<bool> RestartContainersAsync()
        {
            var containersToRestart = new List<string>
            {
                "observer",
                "original"
            };

            var containers =
                (await _dockerClient.Containers
                    .ListContainersAsync(new ContainersListParameters { All = true })
                    .ConfigureAwait(false))
                .Where(i => containersToRestart.Any(j => i.Image.ToLower().Contains(j) ))
                .ToList();

            foreach (var cont in containersToRestart)
            {
                await _dockerClient
                    .Containers
                    .RestartContainerAsync(
                        containers
                            .First(i => i.Image.ToLower().Contains(cont)).ID, 
                        new ContainerRestartParameters { WaitBeforeKillSeconds = 1 }
                    ).ConfigureAwait(false);
            }

            return true;
        }

        public async Task<bool> ResumeContainersAsync()
        {
            const string contStr = "original";

            // Throws exception if container for expected service not found
            var cont =
                (await _dockerClient.Containers
                    .ListContainersAsync(new ContainersListParameters { All = true })
                    .ConfigureAwait(false))
                .First(i => i.Image.Contains(contStr));

            await _dockerClient.Containers
                .UnpauseContainerAsync(cont.ID)
                .ConfigureAwait(false);

            return true;
        }
    }
}