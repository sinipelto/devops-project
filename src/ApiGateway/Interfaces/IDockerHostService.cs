using System.Threading.Tasks;

namespace ApiGateway.Interfaces
{
    public interface IDockerHostService
    {
        public Task<bool> PauseContainersAsync();

        public Task<bool> StopContainersAsync();
        
        public Task<bool> RestartContainersAsync();
        
        public Task<bool> ResumeContainersAsync();
    }
}