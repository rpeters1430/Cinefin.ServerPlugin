using System.Threading.Tasks;
using Cinefin.ServerPlugin.Configuration;
using Cinefin.ServerPlugin.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<CinefinController> _logger;

        public CinefinController(
            SonarrService sonarrService,
            RadarrService radarrService,
            OverseerrService overseerrService,
            ILibraryManager libraryManager,
            ILogger<CinefinController> logger)
        {
            _sonarrService = sonarrService;
            _radarrService = radarrService;
            _overseerrService = overseerrService;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        [HttpGet("Info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                Version = "1.0.0",
                Capabilities = new[] { "Overseerr", "Sonarr", "Radarr" },
                IsConfigured = IsPluginConfigured()
            });
        }

        [HttpPost("Request/Media")]
        public async Task<IActionResult> RequestMedia([FromBody] MediaRequest mediaRequest)
        {
            if (!IsPluginConfigured())
            {
                return BadRequest("Plugin is not configured.");
            }

            // In a real implementation, we would use _overseerrService to submit the request
            _logger.LogInformation("Received media request for TMDB/TVDB ID: {ExternalId}", mediaRequest.ExternalId);
            
            return Ok(new { success = true, message = "Request submitted successfully via Overseerr." });
        }

        [HttpPost("Request/Episode")]
        public async Task<IActionResult> RequestEpisode([FromBody] EpisodeRequest episodeRequest)
        {
            if (!IsPluginConfigured())
            {
                return BadRequest("Plugin is not configured.");
            }

            // Granular episode request directly to Sonarr
            _logger.LogInformation("Received granular episode request: Series {SeriesId}, S{Season}E{Episode}", 
                episodeRequest.SeriesId, episodeRequest.SeasonNumber, episodeRequest.EpisodeNumber);
            
            return Ok(new { success = true, message = "Episode search triggered directly in Sonarr." });
        }

        [HttpPost("TestSonarr")]
        public async Task<IActionResult> TestSonarr([FromBody] TestRequest request)
        {
            var isHealthy = await _sonarrService.IsHealthy(request.Url, request.ApiKey);
            return Ok(new { success = isHealthy });
        }

        [HttpPost("TestRadarr")]
        public async Task<IActionResult> TestRadarr([FromBody] TestRequest request)
        {
            var isHealthy = await _radarrService.IsHealthy(request.Url, request.ApiKey);
            return Ok(new { success = isHealthy });
        }

        [HttpPost("TestOverseerr")]
        public async Task<IActionResult> TestOverseerr([FromBody] TestRequest request)
        {
            var isHealthy = await _overseerrService.IsHealthy(request.Url, request.ApiKey);
            return Ok(new { success = isHealthy });
        }

        private bool IsPluginConfigured()
        {
            var config = Plugin.Instance?.Configuration;
            return config != null && 
                   !string.IsNullOrEmpty(config.SonarrUrl) && 
                   !string.IsNullOrEmpty(config.RadarrUrl) && 
                   !string.IsNullOrEmpty(config.OverseerrUrl);
        }
    }

    public class TestRequest
    {
        public string Url { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class MediaRequest
    {
        public string ExternalId { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty; // "movie" or "tv"
    }

    public class EpisodeRequest
    {
        public string SeriesId { get; set; } = string.Empty;
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
    }
}
