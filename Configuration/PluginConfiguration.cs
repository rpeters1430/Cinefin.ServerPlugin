using MediaBrowser.Model.Plugins;

namespace Cinefin.ServerPlugin.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // External / display URL (what you type in your browser)
        public string SonarrUrl { get; set; } = string.Empty;
        public string SonarrApiKey { get; set; } = string.Empty;
        // Internal URL used by the plugin for server-side API calls (Docker container address)
        public string SonarrInternalUrl { get; set; } = string.Empty;

        public string RadarrUrl { get; set; } = string.Empty;
        public string RadarrApiKey { get; set; } = string.Empty;
        public string RadarrInternalUrl { get; set; } = string.Empty;

        public string OverseerrUrl { get; set; } = string.Empty;
        public string OverseerrApiKey { get; set; } = string.Empty;
        public string OverseerrInternalUrl { get; set; } = string.Empty;

        // Proxy / Basic Auth Support (only applied when going through the external URL)
        public string ProxyUsername { get; set; } = string.Empty;
        public string ProxyPassword { get; set; } = string.Empty;

        // Set true if your reverse proxy uses a self-signed or internal CA certificate
        public bool IgnoreSslErrors { get; set; } = false;

        // Returns the URL the plugin should actually use for API calls.
        // Internal URL takes priority over the external URL when set.
        public string EffectiveSonarrUrl => !string.IsNullOrWhiteSpace(SonarrInternalUrl) ? SonarrInternalUrl : SonarrUrl;
        public string EffectiveRadarrUrl => !string.IsNullOrWhiteSpace(RadarrInternalUrl) ? RadarrInternalUrl : RadarrUrl;
        public string EffectiveOverseerrUrl => !string.IsNullOrWhiteSpace(OverseerrInternalUrl) ? OverseerrInternalUrl : OverseerrUrl;

        // Whether the effective URL goes through the external proxy (proxy auth headers should be sent)
        public bool SonarrUsesProxy => string.IsNullOrWhiteSpace(SonarrInternalUrl);
        public bool RadarrUsesProxy => string.IsNullOrWhiteSpace(RadarrInternalUrl);
        public bool OverseerrUsesProxy => string.IsNullOrWhiteSpace(OverseerrInternalUrl);
    }
}
