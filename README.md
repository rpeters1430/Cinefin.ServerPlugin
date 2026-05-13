# Cinefin Jellyfin Plugin

This plugin integrates Sonarr, Radarr, and Overseerr into Jellyfin.

## Features

- **Sonarr Integration**: Connect to your Sonarr instance.
- **Radarr Integration**: Connect to your Radarr instance.
- **Overseerr Integration**: Connect to your Overseerr instance.
- **Connection Testing**: Test your connections directly from the Jellyfin Dashboard.
- **Scheduled Sync**: Background task to sync requests (example implementation).

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
