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
                sonarrEnabled = !string.IsNullOrEmpty(config.SonarrUrl),
                radarrEnabled = !string.IsNullOrEmpty(config.RadarrUrl),
                overseerrEnabled = !string.IsNullOrEmpty(config.OverseerrUrl)
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
    }
}
