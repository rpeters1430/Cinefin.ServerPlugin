# Cinefin Server Plugin

A secure proxy and orchestration layer for the Cinefin Android app, developed as a Jellyfin server plugin. It integrates Sonarr, Radarr, and Overseerr directly into the Jellyfin environment, eliminating the need for client-side configuration of these services.

## Project Overview

- **Purpose:** Acts as a bridge between the Cinefin Android app and the user's *arr stack (Sonarr, Radarr, Overseerr).
- **Primary Goal:** Security (no need to expose *arr services to the internet) and Zero-Config (credentials are managed on the server).
- **Tech Stack:** C#, .NET 9.0, Jellyfin Server SDK (`MediaBrowser.Controller`).
- **Target Platform:** Jellyfin Server 10.11+.

## Architecture

The plugin follows a service-oriented architecture within the Jellyfin plugin framework:

1.  **API Controllers:** Exposes custom REST endpoints under the `/Cinefin` route for the Android app to consume.
2.  **Domain Services:** Specialized services (`SonarrService`, `RadarrService`, `OverseerrService`) handle the logic for interacting with external APIs.
3.  **HttpClient Integration:** Uses `IHttpClientFactory` with a named client ("cinefin") configured to handle custom SSL validation and Basic Auth for reverse proxies.
4.  **Scheduled Tasks:** Implements `IScheduledTask` for background synchronization (e.g., syncing Overseerr requests).

## Building and Running

### Prerequisites
- .NET 9.0 SDK

### Build
To build the plugin:
```powershell
dotnet build
```

### Deployment
1.  Locate the build output: `bin/Debug/net9.0/Cinefin.ServerPlugin.dll`.
2.  Create a directory named `Cinefin` in your Jellyfin `plugins` folder.
3.  Copy the DLL (and any required dependencies, though usually just the DLL for simple plugins) into that folder.
4.  Restart the Jellyfin server.
5.  Configure the plugin via **Dashboard > Plugins > Cinefin Integration**.

## Project Structure

- `Controllers/`: Contains `CinefinController.cs`, which defines the external API surface.
- `Services/`: Domain logic and API clients for Sonarr, Radarr, and Overseerr. Inherit from `BaseApiService`.
- `Tasks/`: Background operations using Jellyfin's task scheduler.
- `Configuration/`: Defines the plugin's settings (`PluginConfiguration.cs`) and the web UI (`configPage.html`).
- `Plugin.cs`: The plugin entry point and metadata definition.
- `PluginServiceRegistrator.cs`: Handles Dependency Injection registration for the plugin's services.
- `manifest.json`: Metadata for the Jellyfin plugin repository system.

## Development Conventions

- **Dependency Injection:** Use `IPluginServiceRegistrator` to register all services. Inject them into controllers and tasks via constructors.
- **API Endpoints:**
    - Use `[ApiController]` and `[Route("Cinefin")]`.
    - Secure endpoints with `[Authorize]` to ensure only authenticated Jellyfin users can access them.
    - Return `IActionResult` with JSON payloads. Always include a `success` boolean and a `message` for errors.
- **Logging:** Use `ILogger<T>` for consistent logging within the Jellyfin server logs.
- **Service Patterns:** Services should inherit from `BaseApiService` to leverage shared `HttpClient` logic, header management, and error handling.
- **Async/Await:** Use asynchronous patterns (`Task`, `await`) for all I/O-bound operations.

## Key Files

- `Plugin.cs`: Main plugin class.
- `Controllers/CinefinController.cs`: Primary API surface for the mobile app.
- `Services/BaseApiService.cs`: Shared logic for external API communication.
- `CINEFIN_SERVER_PLUGIN_ARCHITECTURE.md`: Detailed architectural vision and roadmap.
