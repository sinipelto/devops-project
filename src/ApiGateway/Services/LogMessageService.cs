using ApiGateway.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ApiGateway.Services
{
    public class LogMessageService : ILogMessageService
    {
        private readonly HttpClient _httpClient;

        public LogMessageService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<string> GetLogMessagesFromHttpServAsync()
        {
            return _httpClient.GetStringAsync(_httpClient.BaseAddress);
        }
    }
}