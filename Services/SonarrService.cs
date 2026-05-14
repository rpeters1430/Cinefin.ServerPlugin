using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public class SonarrSeriesLookup
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("titleSlug")]
        public string TitleSlug { get; set; } = string.Empty;

        [JsonPropertyName("tvdbId")]
        public int TvdbId { get; set; }

        [JsonPropertyName("seriesType")]
        public string SeriesType { get; set; } = "standard";

        [JsonPropertyName("seasons")]
        public List<SonarrSeason> Seasons { get; set; } = new();

        [JsonPropertyName("rootFolderPath")]
        public string? RootFolderPath { get; set; }

        [JsonPropertyName("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonPropertyName("images")]
        public List<object> Images { get; set; } = new();
    }

    public class SonarrSeason
    {
        [JsonPropertyName("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; }
    }

    public class SonarrAddSeriesRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("titleSlug")]
        public string TitleSlug { get; set; } = string.Empty;

        [JsonPropertyName("tvdbId")]
        public int TvdbId { get; set; }

        [JsonPropertyName("seriesType")]
        public string SeriesType { get; set; } = "standard";

        [JsonPropertyName("seasons")]
        public List<SonarrSeason> Seasons { get; set; } = new();

        [JsonPropertyName("rootFolderPath")]
        public string RootFolderPath { get; set; } = string.Empty;

        [JsonPropertyName("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; } = true;

        [JsonPropertyName("addOptions")]
        public SonarrAddOptions AddOptions { get; set; } = new();

        [JsonPropertyName("images")]
        public List<object> Images { get; set; } = new();
    }

    public class SonarrAddOptions
    {
        [JsonPropertyName("searchForMissingEpisodes")]
        public bool SearchForMissingEpisodes { get; set; } = true;
    }

    public class SonarrRootFolder
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }

    public class SonarrQualityProfile
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class SonarrEpisode
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("episodeNumber")]
        public int EpisodeNumber { get; set; }
    }

    public class SonarrEpisodeSearchCommand
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "EpisodeSearch";

        [JsonPropertyName("episodeIds")]
        public List<int> EpisodeIds { get; set; } = new();
    }

    public class SonarrService : BaseApiService
    {
        public SonarrService(IHttpClientFactory httpClientFactory, ILogger<SonarrService> logger) 
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
                Logger.LogInformation("Sonarr v3 status check failed, trying legacy v2 path. Error: {Message}", ex.Message);
                // Fallback to legacy v2 path
                await GetAsync<object>($"{baseUrl}/api/system/status", apiKey, proxyUser, proxyPass);
            }
        }

        public async Task AddSeries(string url, string apiKey, int tvdbId, List<int>? requestedSeasons)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL is required");
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key is required");

            var baseUrl = NormalizeUrl(url);

            // Lookup series by TVDB ID
            var lookupResults = await GetAsync<List<SonarrSeriesLookup>>(
                $"{baseUrl}/api/v3/series/lookup?term=tvdb:{tvdbId}", apiKey);

            if (lookupResults == null || lookupResults.Count == 0)
                throw new InvalidOperationException($"Series with TVDB ID {tvdbId} not found in Sonarr lookup.");

            var series = lookupResults[0];

            // Resolve root folder path
            var rootFolderPath = series.RootFolderPath;
            if (string.IsNullOrWhiteSpace(rootFolderPath))
            {
                var rootFolders = await GetAsync<List<SonarrRootFolder>>($"{baseUrl}/api/v3/rootFolder", apiKey);
                rootFolderPath = rootFolders?.FirstOrDefault()?.Path ?? "/tv";
            }

            // Resolve quality profile
            var qualityProfileId = series.QualityProfileId;
            if (qualityProfileId == 0)
            {
                var profiles = await GetAsync<List<SonarrQualityProfile>>($"{baseUrl}/api/v3/qualityProfile", apiKey);
                qualityProfileId = profiles?.FirstOrDefault()?.Id ?? 1;
            }

            // Build seasons list: all seasons > 0 monitored; season 0 (specials) not monitored.
            // If specific seasons requested, only those seasons are monitored.
            var seasons = series.Seasons.Select(s => new SonarrSeason
            {
                SeasonNumber = s.SeasonNumber,
                Monitored = requestedSeasons != null
                    ? requestedSeasons.Contains(s.SeasonNumber) && s.SeasonNumber > 0
                    : s.SeasonNumber > 0
            }).ToList();

            var addRequest = new SonarrAddSeriesRequest
            {
                Title = series.Title,
                TitleSlug = series.TitleSlug,
                TvdbId = series.TvdbId,
                SeriesType = string.IsNullOrWhiteSpace(series.SeriesType) ? "standard" : series.SeriesType,
                Seasons = seasons,
                RootFolderPath = rootFolderPath,
                QualityProfileId = qualityProfileId,
                Monitored = true,
                AddOptions = new SonarrAddOptions { SearchForMissingEpisodes = true },
                Images = series.Images
            };

            await PostAsync($"{baseUrl}/api/v3/series", apiKey, addRequest);
        }

        public async Task RequestEpisode(string url, string apiKey, int tvdbId, int seasonNumber, int episodeNumber)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL is required");
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key is required");

            var baseUrl = NormalizeUrl(url);

            // Find series in Sonarr by TVDB ID
            var seriesList = await GetAsync<List<SonarrSeriesLookup>>(
                $"{baseUrl}/api/v3/series?tvdbId={tvdbId}", apiKey);

            if (seriesList == null || seriesList.Count == 0)
                throw new InvalidOperationException($"Series with TVDB ID {tvdbId} is not in Sonarr.");

            var series = seriesList[0];

            // Find the specific episode
            var episodes = await GetAsync<List<SonarrEpisode>>(
                $"{baseUrl}/api/v3/episode?seriesId={series.Id}&seasonNumber={seasonNumber}", apiKey);

            var episode = episodes?.FirstOrDefault(e => e.EpisodeNumber == episodeNumber);
            if (episode == null)
                throw new InvalidOperationException(
                    $"Episode S{seasonNumber:D2}E{episodeNumber:D2} not found in Sonarr for series TVDB ID {tvdbId}.");

            // Trigger EpisodeSearch command
            var command = new SonarrEpisodeSearchCommand
            {
                Name = "EpisodeSearch",
                EpisodeIds = new List<int> { episode.Id }
            };
            await PostAsync($"{baseUrl}/api/v3/command", apiKey, command);
        }
    }
}
