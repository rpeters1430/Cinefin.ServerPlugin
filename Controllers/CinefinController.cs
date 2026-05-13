using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Cinefin.ServerPlugin.Configuration;
using Cinefin.ServerPlugin.Services;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Controllers
{
    [ApiController]
    [Route("Cinefin")]
    public class CinefinController : ControllerBase
    {
        private readonly SonarrService _sonarrService;
        private readonly RadarrService _radarrService;
        private readonly OverseerrService _overseerrService;
        private readonly ILogger<CinefinController> _logger;

        public CinefinController(
            SonarrService sonarrService,
            RadarrService radarrService,
            OverseerrService overseerrService,
            ILogger<CinefinController> logger)
        {
            _sonarrService = sonarrService;
            _radarrService = radarrService;
            _overseerrService = overseerrService;
            _logger = logger;
        }

        [HttpGet("Info")]
        public IActionResult GetInfo()
        {
            var config = Plugin.Instance.Configuration;
            return Ok(new
            {
                version = Plugin.Instance.Version.ToString(),
                sonarrEnabled = !string.IsNullOrWhiteSpace(config.SonarrUrl) && !string.IsNullOrWhiteSpace(config.SonarrApiKey),
                radarrEnabled = !string.IsNullOrWhiteSpace(config.RadarrUrl) && !string.IsNullOrWhiteSpace(config.RadarrApiKey),
                overseerrEnabled = !string.IsNullOrWhiteSpace(config.OverseerrUrl) && !string.IsNullOrWhiteSpace(config.OverseerrApiKey)
            });
        }

        [HttpPost("TestSonarr")]
        public async Task<IActionResult> TestSonarr([FromBody] TestRequest request)
        {
            try
            {
                UpdateProxyConfig(request);
                await _sonarrService.ValidateConnection(request.Url, request.ApiKey);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sonarr connection test failed for {Url}", request.Url);
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("TestRadarr")]
        public async Task<IActionResult> TestRadarr([FromBody] TestRequest request)
        {
            try
            {
                UpdateProxyConfig(request);
                await _radarrService.ValidateConnection(request.Url, request.ApiKey);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Radarr connection test failed for {Url}", request.Url);
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("TestOverseerr")]
        public async Task<IActionResult> TestOverseerr([FromBody] TestRequest request)
        {
            try
            {
                UpdateProxyConfig(request);
                await _overseerrService.ValidateConnection(request.Url, request.ApiKey);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Overseerr connection test failed for {Url}", request.Url);
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("RequestMedia")]
        public async Task<IActionResult> RequestMedia([FromBody] MediaRequestBody request)
        {
            try
            {
                var config = Plugin.Instance.Configuration;

                if (!int.TryParse(request.TmdbId, out var tmdbIdInt))
                    return BadRequest(new { success = false, message = $"Invalid TMDB ID format: '{request.TmdbId}'." });

                if (request.MediaType == "movie")
                {
                    if (!string.IsNullOrWhiteSpace(config.OverseerrUrl) && !string.IsNullOrWhiteSpace(config.OverseerrApiKey))
                    {
                        await _overseerrService.RequestMedia(config.OverseerrUrl, config.OverseerrApiKey, tmdbIdInt, request.MediaType, null);
                    }
                    else if (!string.IsNullOrWhiteSpace(config.RadarrUrl) && !string.IsNullOrWhiteSpace(config.RadarrApiKey))
                    {
                        await _radarrService.AddMovie(config.RadarrUrl, config.RadarrApiKey, tmdbIdInt);
                    }
                    else
                    {
                        return StatusCode(503, new { success = false, message = "No movie request service configured." });
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(config.OverseerrUrl) && !string.IsNullOrWhiteSpace(config.OverseerrApiKey))
                    {
                        await _overseerrService.RequestMedia(config.OverseerrUrl, config.OverseerrApiKey, tmdbIdInt, request.MediaType, request.Seasons);
                    }
                    else if (!string.IsNullOrWhiteSpace(config.SonarrUrl) && !string.IsNullOrWhiteSpace(config.SonarrApiKey))
                    {
                        await _sonarrService.AddSeries(config.SonarrUrl, config.SonarrApiKey, tmdbIdInt, request.Seasons);
                    }
                    else
                    {
                        return StatusCode(503, new { success = false, message = "No TV request service configured." });
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request media for TmdbId={TmdbId}", request.TmdbId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("RequestEpisode")]
        public async Task<IActionResult> RequestEpisode([FromBody] EpisodeRequestBody request)
        {
            try
            {
                var config = Plugin.Instance.Configuration;

                if (string.IsNullOrWhiteSpace(config.SonarrUrl) || string.IsNullOrWhiteSpace(config.SonarrApiKey))
                    return StatusCode(503, new { success = false, message = "Sonarr is not configured." });

                await _sonarrService.RequestEpisode(config.SonarrUrl, config.SonarrApiKey, request.TvdbId, request.SeasonNumber, request.EpisodeNumber);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request episode S{Season}E{Episode} for TvdbId={TvdbId}",
                    request.SeasonNumber, request.EpisodeNumber, request.TvdbId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private void UpdateProxyConfig(TestRequest request)
        {
            // Temporarily update the plugin instance config for the duration of the request/service call
            // Since BaseApiService reads from Plugin.Instance.Configuration
            // Note: This is a bit hacky but works for the test connection flow
            Plugin.Instance.Configuration.ProxyUsername = request.ProxyUsername;
            Plugin.Instance.Configuration.ProxyPassword = request.ProxyPassword;
        }

        public class TestRequest
        {
            public string Url { get; set; } = string.Empty;
            public string ApiKey { get; set; } = string.Empty;
            public string ProxyUsername { get; set; } = string.Empty;
            public string ProxyPassword { get; set; } = string.Empty;
        }

        public class MediaRequestBody
        {
            public string TmdbId { get; set; } = string.Empty;
            public string MediaType { get; set; } = string.Empty;
            public List<int>? Seasons { get; set; }
        }

        public class EpisodeRequestBody
        {
            public int TvdbId { get; set; }
            public int SeasonNumber { get; set; }
            public int EpisodeNumber { get; set; }
        }
    }
}
