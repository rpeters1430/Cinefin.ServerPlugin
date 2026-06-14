# Cinefin Jellyfin Plugin

This plugin integrates Sonarr, Radarr, and Overseerr into Jellyfin.

## Features

- **Sonarr Integration**: Connect to your Sonarr instance.
- **Radarr Integration**: Connect to your Radarr instance.
- **Overseerr Integration**: Connect to your Overseerr instance.
- **Credential Sync**: Cinefin clients can import the saved external URLs and API keys from Jellyfin via `GET /Cinefin/Credentials`.
- **Connection Testing**: Test your connections directly from the Jellyfin Dashboard.
- **Scheduled Sync**: Background task to sync requests (example implementation).

## Cinefin App Credential Sync

After the plugin configuration is saved, authenticated Cinefin clients can call `GET /Cinefin/Credentials` on the Jellyfin server. The endpoint returns the saved external URLs and API keys without testing the upstream services, so credentials can be synced to a device even if Sonarr, Radarr, or Overseerr are temporarily offline.

Example response:

```json
{
  "sonarr": { "url": "https://example.com/sonarr", "apiKey": "...", "isConfigured": true },
  "radarr": { "url": "https://example.com/radarr", "apiKey": "...", "isConfigured": true },
  "overseerr": { "url": "https://example.com/overseerr", "apiKey": "...", "isConfigured": true },
  "proxy": { "username": "", "password": "", "isConfigured": false },
  "ignoreSslErrors": false
}
```

## Installation

1.  Build the project:
    ```bash
    dotnet build
    ```
2.  Copy `bin/Debug/net9.0/Cinefin.ServerPlugin.dll` to your Jellyfin `plugins/Cinefin` directory.
3.  Restart Jellyfin.
4.  Go to **Dashboard > Plugins > Cinefin Integration** to configure your API keys and URLs.

## Development

- Targets .NET 9.0 (net9.0) for Jellyfin 10.11+ compatibility.
- Uses `IPluginServiceRegistrator` for dependency injection.
- Employs `IScheduledTask` for background operations.
