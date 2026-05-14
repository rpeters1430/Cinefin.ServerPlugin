using MediaBrowser.Model.Plugins;

namespace Cinefin.ServerPlugin.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string SonarrUrl { get; set; } = string.Empty;
        public string SonarrApiKey { get; set; } = string.Empty;
        
        public string RadarrUrl { get; set; } = string.Empty;
        public string RadarrApiKey { get; set; } = string.Empty;
        
        public string OverseerrUrl { get; set; } = string.Empty;
        public string OverseerrApiKey { get; set; } = string.Empty;

        // Proxy / Basic Auth Support
        public string ProxyUsername { get; set; } = string.Empty;
        public string ProxyPassword { get; set; } = string.Empty;

        // Set true if your reverse proxy uses a self-signed or internal CA certificate
        public bool IgnoreSslErrors { get; set; } = false;
    }
}
