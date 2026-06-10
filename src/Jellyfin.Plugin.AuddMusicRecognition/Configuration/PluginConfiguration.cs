using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AuddMusicRecognition.Configuration;

/// <summary>
/// Persistent plugin settings.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the AudD API token.
    /// </summary>
    public string AuddApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how many seconds before the current playback position are included.
    /// </summary>
    public int PreRollSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets how many seconds after the current playback position are included.
    /// </summary>
    public int PostRollSeconds { get; set; } = 8;

    /// <summary>
    /// Gets or sets the maximum extracted clip length in seconds.
    /// </summary>
    public int MaxClipSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the optional comma-separated AudD metadata return list.
    /// </summary>
    public string ReturnMetadata { get; set; } = "spotify,apple_music";

    /// <summary>
    /// Gets or sets the FFmpeg executable path.
    /// </summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>
    /// Gets or sets a value indicating whether the player overlay is injected into Jellyfin Web.
    /// </summary>
    public bool EnableWebOverlay { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the player overlay shows sent clip diagnostics.
    /// </summary>
    public bool ShowOverlayDebugInfo { get; set; }
}
