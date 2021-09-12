using System.Net.Http;
using System.Threading.Tasks;

namespace CommonTools.Services
{
    public static class HttpClientFactory
    {
        public static readonly HttpClient Client;

        static HttpClientFactory()
        {
            var handler = new HttpClientHandler();
            Client ??= new HttpClient(handler);
        }

        public static async Task<string> Get<T>(string url)
        {
            var result = await Client.GetAsync(url).ConfigureAwait(false);
            return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}