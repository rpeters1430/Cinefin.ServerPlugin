using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public class OverseerrService : BaseApiService
    {
        public OverseerrService(IHttpClientFactory httpClientFactory, ILogger<OverseerrService> logger) 
            : base(httpClientFactory, logger)
        {
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
                var baseUrl = url.TrimEnd('/');
                // Overseerr uses /api/v1/status
                var result = await GetAsync<object>($"{baseUrl}/api/v1/status", apiKey);
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
