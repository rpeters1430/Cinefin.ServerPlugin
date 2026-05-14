using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public class OverseerrRequestPayload
    {
        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = string.Empty;

        [JsonPropertyName("mediaId")]
        public int MediaId { get; set; }

        [JsonPropertyName("seasons")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Seasons { get; set; }
    }

    public class OverseerrService : BaseApiService
    {
        public OverseerrService(IHttpClientFactory httpClientFactory, ILogger<OverseerrService> logger) 
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

        public async Task ValidateConnection(string url, string apiKey, string? proxyUser = null, string? proxyPass = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL is required");
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key is required");

            var baseUrl = NormalizeUrl(url);
            await GetAsync<object>($"{baseUrl}/api/v1/status", apiKey, proxyUser, proxyPass);
        }

        public async Task RequestMedia(string url, string apiKey, int tmdbId, string mediaType, List<int>? seasons)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL is required");
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key is required");

            var baseUrl = NormalizeUrl(url);
            var payload = new OverseerrRequestPayload
            {
                MediaType = mediaType,
                MediaId = tmdbId,
                Seasons = seasons
            };
            await PostAsync($"{baseUrl}/api/v1/request", apiKey, payload);
        }
    }
}
