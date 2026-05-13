using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public class RadarrService : BaseApiService
    {
        private readonly ILogger<RadarrService> _logger;

        public RadarrService(IHttpClientFactory httpClientFactory, ILogger<RadarrService> logger) 
            : base(httpClientFactory)
        {
            _logger = logger;
        }

        public async Task<bool> IsHealthy()
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.RadarrUrl) || string.IsNullOrEmpty(config.RadarrApiKey))
            {
                return false;
            }
            return await IsHealthy(config.RadarrUrl, config.RadarrApiKey);
        }

        public async Task<bool> IsHealthy(string url, string apiKey)
        {
            try
            {
                var result = await GetAsync<object>($"{url}/api/v3/system/status", apiKey);
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
