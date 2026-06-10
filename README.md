# Jellyfin Music Recognition

Jellyfin server plugin plus a small Jellyfin Web runtime module for recognizing music playing inside videos. The server plugin extracts a short audio clip around the current playback position, uploads it to the selected recognition provider, and returns normalized track metadata to the web overlay.

## Repository Layout

- `src/Jellyfin.Plugin.AuddMusicRecognition` - Jellyfin server plugin.
- `tests/Jellyfin.Plugin.AuddMusicRecognition.Tests` - unit tests for clip windows and provider response parsing.
- `web-client` - Jellyfin Web runtime module/patch asset for the player overlay.

## Build

The project targets the current Jellyfin plugin template shape: .NET 9 and Jellyfin `10.11.3` packages.

```powershell
dotnet restore .\src\Jellyfin.Plugin.AuddMusicRecognition\Jellyfin.Plugin.AuddMusicRecognition.csproj
dotnet test .\tests\Jellyfin.Plugin.AuddMusicRecognition.Tests\Jellyfin.Plugin.AuddMusicRecognition.Tests.csproj
dotnet publish .\src\Jellyfin.Plugin.AuddMusicRecognition\Jellyfin.Plugin.AuddMusicRecognition.csproj -c Release
```

Copy the publish output into a Jellyfin plugin folder, restart Jellyfin, then open the plugin settings page and choose a provider:

- AudD: set the AudD API token.
- Shazam (RapidAPI): set the RapidAPI key for `shazam.p.rapidapi.com`.

## Jellyfin Web Overlay

The server plugin automatically injects the player overlay into Jellyfin Web when `EnableWebOverlay` is enabled in plugin settings. It registers FileTransformation callbacks for both `index.html` and `main.jellyfin.bundle.js`, so the overlay can still load when Jellyfin Web or the browser caches the HTML entrypoint. The standalone script is also served from:

```text
/Plugins/AuddMusicRecognition/Web/audd-music-recognition.plugin.js
```

The injector uses a unique `data-audd-music-recognition="player-overlay"` marker and `auddRecognition*` CSS classes, so it does not modify Intro Skipper classes, media segment buttons, or branding CSS. `web-client/audd-music-recognition.plugin.js` is kept as the source asset that gets embedded into the plugin DLL.

Enable `Show sent clip test data in player overlay` in plugin settings to show the extracted clip duration and uploaded file size next to the player button.

## GitHub Release Repository

This repository includes a Jellyfin repository manifest and a GitHub Actions release workflow.

1. Push this project to GitHub.
2. Open Actions, run `Release Jellyfin Plugin`, and set `version`, for example `1.0.0.0`.
3. The workflow builds/tests the plugin, publishes a release zip, updates `manifest.json`, and commits it back.
4. In Jellyfin, add this repository URL:

```text
https://raw.githubusercontent.com/OWNER/REPO/main/manifest.json
```

Replace `OWNER/REPO` with your GitHub repository path after you upload the code.

## Notes

- The endpoint is `POST /Plugins/AuddMusicRecognition/Recognize`.
- Recognition is manual only in v1: the overlay button triggers the request.
- Providers supported in settings: AudD and Shazam through RapidAPI.
- The default clip window is 5 seconds before and 8 seconds after the current position, capped by `MaxClipSeconds`.
- FFmpeg must be available to the Jellyfin server process. Set `FfmpegPath` in plugin settings if it is not on PATH.
- Jellyfin's plugin manifest installs the server plugin. The server plugin injects the web player overlay automatically for standard Jellyfin Web clients.
