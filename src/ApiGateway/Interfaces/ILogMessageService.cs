using System.Threading.Tasks;

namespace ApiGateway.Interfaces
{
    public interface ILogMessageService
    {
        public Task<string> GetLogMessagesFromHttpServAsync();
    }
}