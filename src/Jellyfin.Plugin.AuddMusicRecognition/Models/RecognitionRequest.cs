using System;

namespace Jellyfin.Plugin.AuddMusicRecognition.Models;

/// <summary>
/// Request body for music recognition.
/// </summary>
public sealed class RecognitionRequest
{
    /// <summary>
    /// Gets or sets the Jellyfin item id.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the optional media source id from the web player.
    /// </summary>
    public string? MediaSourceId { get; set; }

    /// <summary>
    /// Gets or sets the current playback position in Jellyfin ticks.
    /// </summary>
    public long PositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the optional FFmpeg/Jellyfin audio stream index.
    /// </summary>
    public int? AudioStreamIndex { get; set; }
}
