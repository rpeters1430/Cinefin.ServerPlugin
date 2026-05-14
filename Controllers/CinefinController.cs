using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
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

        /// <summary>
        /// Tests TCP reachability and HTTP connectivity for all configured services.
        /// Returns actionable diagnostics to help identify Docker networking / SSL issues.
        /// </summary>
        [HttpGet("Diagnostics")]
        public async Task<IActionResult> GetDiagnostics()
        {
            var config = Plugin.Instance!.Configuration;
            var results = new List<object>();

            async Task<object> Probe(string name, string effectiveUrl, string externalUrl)
            {
                if (string.IsNullOrWhiteSpace(effectiveUrl))
                    return new { service = name, skipped = true, reason = "Not configured" };

                var usingInternal = effectiveUrl != externalUrl;
                string? tcpError = null;
                string? httpError = null;
                bool tcpOk = false;
                bool httpOk = false;

                try
                {
                    var uri = new Uri(effectiveUrl);
                    var port = uri.Port > 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(uri.Host, port).WaitAsync(TimeSpan.FromSeconds(5));
                    tcpOk = true;
                }
                catch (Exception ex)
                {
                    tcpError = ex.Message;
                }

                if (tcpOk)
                {
                    try
                    {
                        using var http = new HttpClient(new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                            AllowAutoRedirect = true,
                        });
                        http.Timeout = TimeSpan.FromSeconds(8);
                        var response = await http.GetAsync(effectiveUrl);
                        httpOk = (int)response.StatusCode < 500;
                        if (!httpOk) httpError = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                    }
                    catch (Exception ex)
                    {
                        httpError = ex.Message;
                    }
                }

                return new
                {
                    service = name,
                    effectiveUrl,
                    externalUrl,
                    usingInternalUrl = usingInternal,
                    tcpReachable = tcpOk,
                    tcpError,
                    httpReachable = httpOk,
                    httpError,
                    suggestion = (!tcpOk)
                        ? (usingInternal
                            ? "TCP failed on internal URL. Check Docker container name/network and that the port is correct."
                            : "TCP failed on external URL. For Docker setups, set an Internal URL using the container name (e.g. http://sonarr:8989/sonarr).")
                        : (!httpOk
                            ? "TCP succeeded but HTTP failed. Check the base path and API key."
                            : null)
                };
            }

            results.Add(await Probe("Sonarr", config.EffectiveSonarrUrl, config.SonarrUrl));
            results.Add(await Probe("Radarr", config.EffectiveRadarrUrl, config.RadarrUrl));
            results.Add(await Probe("Overseerr", config.EffectiveOverseerrUrl, config.OverseerrUrl));

            return Ok(new { diagnostics = results });
        }

        [HttpPost("TestSonarr")]
        public async Task<IActionResult> TestSonarr([FromBody] TestRequest request)
        {
            var config = Plugin.Instance!.Configuration;
            var oldIgnoreSsl = config.IgnoreSslErrors;
            try
            {
                config.IgnoreSslErrors = request.IgnoreSsl;
                await _sonarrService.ValidateConnection(request.Url, request.ApiKey, request.ProxyUsername, request.ProxyPassword);
                return Ok(new { success = true, message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sonarr connection test failed for {Url}", request.Url);
                return Ok(new { success = false, message = ex.Message });
            }
            finally
            {
                config.IgnoreSslErrors = oldIgnoreSsl;
            }
        }

        [HttpPost("TestRadarr")]
        public async Task<IActionResult> TestRadarr([FromBody] TestRequest request)
        {
            var config = Plugin.Instance!.Configuration;
            var oldIgnoreSsl = config.IgnoreSslErrors;
            try
            {
                config.IgnoreSslErrors = request.IgnoreSsl;
                await _radarrService.ValidateConnection(request.Url, request.ApiKey, request.ProxyUsername, request.ProxyPassword);
                return Ok(new { success = true, message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Radarr connection test failed for {Url}", request.Url);
                return Ok(new { success = false, message = ex.Message });
            }
            finally
            {
                config.IgnoreSslErrors = oldIgnoreSsl;
            }
        }

        [HttpPost("TestOverseerr")]
        public async Task<IActionResult> TestOverseerr([FromBody] TestRequest request)
        {
            var config = Plugin.Instance!.Configuration;
            var oldIgnoreSsl = config.IgnoreSslErrors;
            try
            {
                config.IgnoreSslErrors = request.IgnoreSsl;
                await _overseerrService.ValidateConnection(request.Url, request.ApiKey, request.ProxyUsername, request.ProxyPassword);
                return Ok(new { success = true, message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Overseerr connection test failed for {Url}", request.Url);
                return Ok(new { success = false, message = ex.Message });
            }
            finally
            {
                config.IgnoreSslErrors = oldIgnoreSsl;
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

                // Use internal URLs (Docker container addresses) when configured — avoids
                // hairpin NAT failures when the Jellyfin container can't reach the external proxy URL.
                // Proxy auth headers are only sent when going through the external URL.
                string? proxyUser = null, proxyPass = null;

                if (request.MediaType == "movie")
                {
                    if (!string.IsNullOrWhiteSpace(config.OverseerrUrl) && !string.IsNullOrWhiteSpace(config.OverseerrApiKey))
                    {
                        if (config.OverseerrUsesProxy) { proxyUser = config.ProxyUsername; proxyPass = config.ProxyPassword; }
                        await _overseerrService.RequestMedia(config.EffectiveOverseerrUrl, config.OverseerrApiKey, externalIdInt, request.MediaType, null, proxyUser, proxyPass);
                    }
                    else if (!string.IsNullOrWhiteSpace(config.RadarrUrl) && !string.IsNullOrWhiteSpace(config.RadarrApiKey))
                    {
                        if (config.RadarrUsesProxy) { proxyUser = config.ProxyUsername; proxyPass = config.ProxyPassword; }
                        await _radarrService.AddMovie(config.EffectiveRadarrUrl, config.RadarrApiKey, externalIdInt, proxyUser, proxyPass);
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
                        if (config.OverseerrUsesProxy) { proxyUser = config.ProxyUsername; proxyPass = config.ProxyPassword; }
                        await _overseerrService.RequestMedia(config.EffectiveOverseerrUrl, config.OverseerrApiKey, externalIdInt, request.MediaType, request.Seasons, proxyUser, proxyPass);
                    }
                    else if (!string.IsNullOrWhiteSpace(config.SonarrUrl) && !string.IsNullOrWhiteSpace(config.SonarrApiKey))
                    {
                        if (config.SonarrUsesProxy) { proxyUser = config.ProxyUsername; proxyPass = config.ProxyPassword; }
                        await _sonarrService.AddSeries(config.EffectiveSonarrUrl, config.SonarrApiKey, externalIdInt, request.Seasons, proxyUser, proxyPass);
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

                string? epProxyUser = config.SonarrUsesProxy ? config.ProxyUsername : null;
                string? epProxyPass = config.SonarrUsesProxy ? config.ProxyPassword : null;
                await _sonarrService.RequestEpisode(config.EffectiveSonarrUrl, config.SonarrApiKey, seriesIdInt, request.SeasonNumber, request.EpisodeNumber, epProxyUser, epProxyPass);
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
            public bool IgnoreSsl { get; set; }
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
