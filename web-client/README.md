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
- Sends `itemId`, `mediaSourceId`, `positionTicks`, and `audioStreamIndex` to `POST /Plugins/AuddMusicRecognition/Recognize`.
- Falls back to Jellyfin video/HLS URLs when the Jellyfin Web playback manager is not exposed as a window global.
- Caches results in memory by item id and rounded 10-second playback position.
- Shows loading, recognized track, no match, and error states.
