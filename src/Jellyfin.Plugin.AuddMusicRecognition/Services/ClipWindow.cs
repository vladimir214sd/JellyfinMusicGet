using System;
using System.Globalization;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Audio extraction window expressed in Jellyfin ticks.
/// </summary>
public sealed record ClipWindow(long StartTicks, long EndTicks)
{
    /// <summary>
    /// Gets the clip duration in ticks.
    /// </summary>
    public long DurationTicks => Math.Max(0, EndTicks - StartTicks);

    /// <summary>
    /// Gets the FFmpeg start timestamp in seconds.
    /// </summary>
    public string StartSeconds => ToSeconds(StartTicks);

    /// <summary>
    /// Gets the FFmpeg duration in seconds.
    /// </summary>
    public string DurationSeconds => ToSeconds(DurationTicks);

    private static string ToSeconds(long ticks)
    {
        return TimeSpan.FromTicks(ticks).TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
