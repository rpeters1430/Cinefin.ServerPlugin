using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public class RadarrMovieLookup
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("titleSlug")]
        public string TitleSlug { get; set; } = string.Empty;

        [JsonPropertyName("tmdbId")]
        public int TmdbId { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("images")]
        public List<object> Images { get; set; } = new();
    }

    public class RadarrAddMovieRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("titleSlug")]
        public string TitleSlug { get; set; } = string.Empty;

        [JsonPropertyName("tmdbId")]
        public int TmdbId { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("images")]
        public List<object> Images { get; set; } = new();

        [JsonPropertyName("rootFolderPath")]
        public string RootFolderPath { get; set; } = string.Empty;

        [JsonPropertyName("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; } = true;

        [JsonPropertyName("addOptions")]
        public RadarrAddOptions AddOptions { get; set; } = new();
    }

    public class RadarrAddOptions
    {
        [JsonPropertyName("searchForMovie")]
        public bool SearchForMovie { get; set; } = true;
    }

    public class RadarrRootFolder
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }

    public class RadarrQualityProfile
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

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

        public async Task ValidateConnection(string url, string apiKey, string? proxyUser = null, string? proxyPass = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL is required");
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key is required");

            var baseUrl = NormalizeUrl(url);
            
            try
            {
                // Try standard v3 path first
                await GetAsync<object>($"{baseUrl}/api/v3/system/status", apiKey, proxyUser, proxyPass);
            }
            catch (Exception ex)
            {
                Logger.LogInformation("Radarr v3 status check failed, trying legacy v2 path. Error: {Message}", ex.Message);
                // Fallback to legacy v2 path
                await GetAsync<object>($"{baseUrl}/api/system/status", apiKey, proxyUser, proxyPass);
            }
        }

        public async Task AddMovie(string url, string apiKey, int tmdbId)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL is required");
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key is required");

            var baseUrl = NormalizeUrl(url);

            // Lookup movie using the tmdb: term pattern (consistent with Sonarr, more widely supported)
            var lookupResults = await GetAsync<List<RadarrMovieLookup>>(
                $"{baseUrl}/api/v3/movie/lookup?term=tmdb:{tmdbId}", apiKey);

            if (lookupResults == null || lookupResults.Count == 0)
                throw new InvalidOperationException($"Movie with TMDB ID {tmdbId} not found in Radarr lookup.");

            var movie = lookupResults[0];

            // Resolve root folder path
            var rootFolders = await GetAsync<List<RadarrRootFolder>>($"{baseUrl}/api/v3/rootFolder", apiKey);
            var rootFolderPath = rootFolders?.FirstOrDefault()?.Path ?? "/movies";

            // Resolve quality profile
            var profiles = await GetAsync<List<RadarrQualityProfile>>($"{baseUrl}/api/v3/qualityProfile", apiKey);
            var qualityProfileId = profiles?.FirstOrDefault()?.Id ?? 1;

            var addRequest = new RadarrAddMovieRequest
            {
                Title = movie.Title,
                TitleSlug = movie.TitleSlug,
                TmdbId = movie.TmdbId,
                Year = movie.Year,
                Images = movie.Images,
                RootFolderPath = rootFolderPath,
                QualityProfileId = qualityProfileId,
                Monitored = true,
                AddOptions = new RadarrAddOptions { SearchForMovie = true }
            };

            await PostAsync($"{baseUrl}/api/v3/movie", apiKey, addRequest);
        }
    }
}
