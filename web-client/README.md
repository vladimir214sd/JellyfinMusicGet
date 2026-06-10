# Jellyfin Web Overlay Module

`audd-music-recognition.plugin.js` exposes a window-global Jellyfin Web plugin named `AuddMusicRecognitionPlugin` and also self-starts when loaded directly by the server plugin's HTML injector.

## Server Plugin Loading

The Jellyfin server plugin embeds this file and serves it from:

```text
/Plugins/AuddMusicRecognition/Web/audd-music-recognition.plugin.js
```

It is injected into Jellyfin Web by `WebOverlayInjectionMiddleware` with a unique script marker. You only need to load this file manually if you disable automatic injection or maintain a custom Jellyfin Web build.

## Behavior

- Adds a compact overlay in the top-right of the active video player.
- Shows the overlay only while playback is paused, so it does not cover the video during viewing.
- Sends `itemId`, `mediaSourceId`, optional `mediaSourcePath`, `positionTicks`, and `audioStreamIndex` to `POST /Plugins/AuddMusicRecognition/Recognize`.
- Reads Jellyfin PlaybackInfo before recognition to recover missing media source ids and local paths.
- Falls back to Jellyfin video/HLS URLs when the Jellyfin Web playback manager is not exposed as a window global.
- When `ShowOverlayDebugInfo` is enabled in plugin settings, displays the extracted clip duration and uploaded file size next to the button.
- Caches results in memory by item id and rounded 10-second playback position.
- Clears the displayed track when playback moves more than 60 seconds away from the recognized position.
- Shows loading, recognized track, no match, and error states in a compact now-playing card.
- Uses Spotify first, then Apple Music, then AudD song links when metadata is available.
- Supports copying the displayed track text from the card.
- Localizes overlay labels from the Jellyfin/browser language with English fallback.
