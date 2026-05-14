using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cinefin.ServerPlugin.Configuration;
using Cinefin.ServerPlugin.Services;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Controllers
{
    [ApiController]
    [Route("Cinefin")]
    [Authorize]
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
            var plugin = Plugin.Instance!;
            var config = plugin.Configuration;
            var capabilities = new List<string>();
            
            bool sonarrEnabled = !string.IsNullOrWhiteSpace(config.SonarrUrl) && !string.IsNullOrWhiteSpace(config.SonarrApiKey);
            bool radarrEnabled = !string.IsNullOrWhiteSpace(config.RadarrUrl) && !string.IsNullOrWhiteSpace(config.RadarrApiKey);
            bool overseerrEnabled = !string.IsNullOrWhiteSpace(config.OverseerrUrl) && !string.IsNullOrWhiteSpace(config.OverseerrApiKey);

            if (sonarrEnabled) capabilities.Add("sonarr");
            if (radarrEnabled) capabilities.Add("radarr");
            if (overseerrEnabled) capabilities.Add("overseerr");

            return Ok(new
            {
                version = plugin.Version.ToString(),
                capabilities = capabilities,
                isConfigured = sonarrEnabled || radarrEnabled || overseerrEnabled
            });
        }

        [HttpPost("TestSonarr")]
        public async Task<IActionResult> TestSonarr([FromBody] TestRequest request)
        {
            try
            {
                await _sonarrService.ValidateConnection(request.Url, request.ApiKey, request.ProxyUsername, request.ProxyPassword);
                return Ok(new { success = true, message = "Connection successful" });
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
                await _radarrService.ValidateConnection(request.Url, request.ApiKey, request.ProxyUsername, request.ProxyPassword);
                return Ok(new { success = true, message = "Connection successful" });
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
                await _overseerrService.ValidateConnection(request.Url, request.ApiKey, request.ProxyUsername, request.ProxyPassword);
                return Ok(new { success = true, message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Overseerr connection test failed for {Url}", request.Url);
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Request/Media")]
        public async Task<IActionResult> RequestMedia([FromBody] MediaRequestBody request)
        {
            try
            {
                var config = Plugin.Instance!.Configuration;

                if (!int.TryParse(request.ExternalId, out var externalIdInt))
                    return BadRequest(new { success = false, message = $"Invalid ID format: '{request.ExternalId}'." });

                if (request.MediaType == "movie")
                {
                    if (!string.IsNullOrWhiteSpace(config.OverseerrUrl) && !string.IsNullOrWhiteSpace(config.OverseerrApiKey))
                    {
                        await _overseerrService.RequestMedia(config.OverseerrUrl, config.OverseerrApiKey, externalIdInt, request.MediaType, null);
                    }
                    else if (!string.IsNullOrWhiteSpace(config.RadarrUrl) && !string.IsNullOrWhiteSpace(config.RadarrApiKey))
                    {
                        await _radarrService.AddMovie(config.RadarrUrl, config.RadarrApiKey, externalIdInt);
                    }
                    else
                    {
                        return Ok(new { success = false, message = "No movie request service configured on server." });
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(config.OverseerrUrl) && !string.IsNullOrWhiteSpace(config.OverseerrApiKey))
                    {
                        await _overseerrService.RequestMedia(config.OverseerrUrl, config.OverseerrApiKey, externalIdInt, request.MediaType, request.Seasons);
                    }
                    else if (!string.IsNullOrWhiteSpace(config.SonarrUrl) && !string.IsNullOrWhiteSpace(config.SonarrApiKey))
                    {
                        await _sonarrService.AddSeries(config.SonarrUrl, config.SonarrApiKey, externalIdInt, request.Seasons);
                    }
                    else
                    {
                        return Ok(new { success = false, message = "No TV request service configured on server." });
                    }
                }

                return Ok(new { success = true, message = "Request submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request media for ExternalId={ExternalId}", request.ExternalId);
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Request/Episode")]
        public async Task<IActionResult> RequestEpisode([FromBody] EpisodeRequestBody request)
        {
            try
            {
                var config = Plugin.Instance!.Configuration;

                if (string.IsNullOrWhiteSpace(config.SonarrUrl) || string.IsNullOrWhiteSpace(config.SonarrApiKey))
                    return Ok(new { success = false, message = "Sonarr is not configured on server." });

                if (!int.TryParse(request.SeriesId, out var seriesIdInt))
                    return BadRequest(new { success = false, message = $"Invalid series ID format: '{request.SeriesId}'." });

                await _sonarrService.RequestEpisode(config.SonarrUrl, config.SonarrApiKey, seriesIdInt, request.SeasonNumber, request.EpisodeNumber);
                return Ok(new { success = true, message = "Episode search triggered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request episode S{Season}E{Episode} for SeriesId={SeriesId}",
                    request.SeasonNumber, request.EpisodeNumber, request.SeriesId);
                return Ok(new { success = false, message = ex.Message });
            }
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
            public string ExternalId { get; set; } = string.Empty;
            public string MediaType { get; set; } = string.Empty;
            public List<int>? Seasons { get; set; }
        }

        public class EpisodeRequestBody
        {
            public string SeriesId { get; set; } = string.Empty;
            public int SeasonNumber { get; set; }
            public int EpisodeNumber { get; set; }
        }
    }
}
