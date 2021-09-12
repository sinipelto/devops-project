using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace CommonTools.Utils
{
    public class ConfigurationTools
    {
        public static T ReadConfiguration<T>(string path, IFileProvider provider = null, bool optional = false, bool reload = false)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(provider, path, optional, reload)
                .Build();

            return configuration.Get<T>();
        }
        
        public static IConfiguration GetConfiguration(string path, IFileProvider provider = null, bool optional = false, bool reload = false)
        {
            return new ConfigurationBuilder()
                .AddJsonFile(provider, path, optional, reload)
                .Build();
        }
    }
}
