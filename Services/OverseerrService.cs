using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public class OverseerrService : BaseApiService
    {
        private readonly ILogger<OverseerrService> _logger;

        public OverseerrService(IHttpClientFactory httpClientFactory, ILogger<OverseerrService> logger) 
            : base(httpClientFactory)
        {
            _logger = logger;
        }

        public async Task<bool> IsHealthy()
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.OverseerrUrl) || string.IsNullOrEmpty(config.OverseerrApiKey))
            {
                return false;
            }
            return await IsHealthy(config.OverseerrUrl, config.OverseerrApiKey);
        }

        public async Task<bool> IsHealthy(string url, string apiKey)
        {
            try
            {
                // Overseerr uses /api/v1/settings/main for health check usually or /api/v1/status
                var result = await GetAsync<object>($"{url}/api/v1/status", apiKey);
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
