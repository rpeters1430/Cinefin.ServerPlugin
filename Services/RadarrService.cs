using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public class RadarrService : BaseApiService
    {
        public RadarrService(IHttpClientFactory httpClientFactory, ILogger<RadarrService> logger) 
            : base(httpClientFactory, logger)
        {
        }

        public async Task<bool> IsHealthy(string url, string apiKey)
        {
            try
            {
                await ValidateConnection(url, apiKey);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task ValidateConnection(string url, string apiKey)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentException("URL is required");
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentException("API Key is required");

            var baseUrl = url.TrimEnd('/');
            // Try v3 status endpoint
            await GetAsync<object>($"{baseUrl}/api/v3/system/status", apiKey);
        }
    }
}
