using System;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Computes the media window sent to AudD.
/// </summary>
internal static class ClipWindowCalculator
{
    private const int MinimumClipSeconds = 1;

    /// <summary>
    /// Calculates a clip window around the playback position.
    /// </summary>
    /// <param name="positionTicks">Current playback position in ticks.</param>
    /// <param name="runtimeTicks">Optional media runtime in ticks.</param>
    /// <param name="preRollSeconds">Seconds before the current position.</param>
    /// <param name="postRollSeconds">Seconds after the current position.</param>
    /// <param name="maxClipSeconds">Maximum clip length in seconds.</param>
    /// <returns>The clamped clip window.</returns>
    public static ClipWindow Calculate(
        long positionTicks,
        long? runtimeTicks,
        int preRollSeconds,
        int postRollSeconds,
        int maxClipSeconds)
    {
        var runtime = runtimeTicks.GetValueOrDefault() > 0 ? runtimeTicks : null;
        var position = Math.Max(0, positionTicks);

        if (runtime.HasValue)
        {
            position = Math.Min(position, runtime.Value);
        }

        var maxTicks = SecondsToTicks(Math.Max(MinimumClipSeconds, maxClipSeconds));
        var preTicks = SecondsToTicks(Math.Max(0, preRollSeconds));
        var postTicks = SecondsToTicks(Math.Max(0, postRollSeconds));
        var (beforeTicks, afterTicks) = ConstrainAroundPosition(preTicks, postTicks, maxTicks);

        var startTicks = position - beforeTicks;
        var endTicks = position + afterTicks;

        if (startTicks < 0)
        {
            endTicks -= startTicks;
            startTicks = 0;
        }

        if (runtime.HasValue && endTicks > runtime.Value)
        {
            var overflow = endTicks - runtime.Value;
            startTicks = Math.Max(0, startTicks - overflow);
            endTicks = runtime.Value;
        }

        if (endTicks - startTicks > maxTicks)
        {
            endTicks = startTicks + maxTicks;

            if (runtime.HasValue && endTicks > runtime.Value)
            {
                endTicks = runtime.Value;
                startTicks = Math.Max(0, endTicks - maxTicks);
            }
        }

        if (endTicks <= startTicks)
        {
            endTicks = runtime.HasValue
                ? Math.Min(runtime.Value, startTicks + Math.Min(maxTicks, TimeSpan.TicksPerSecond))
                : startTicks + Math.Min(maxTicks, TimeSpan.TicksPerSecond);
        }

        return new ClipWindow(startTicks, endTicks);
    }

    private static (long BeforeTicks, long AfterTicks) ConstrainAroundPosition(long preTicks, long postTicks, long maxTicks)
    {
        var requestedTicks = preTicks + postTicks;

        if (requestedTicks <= 0)
        {
            return (0, maxTicks);
        }

        if (requestedTicks <= maxTicks)
        {
            return (preTicks, postTicks);
        }

        var beforeTicks = (long)Math.Round(maxTicks * (preTicks / (double)requestedTicks), MidpointRounding.AwayFromZero);
        beforeTicks = Math.Min(preTicks, Math.Max(0, beforeTicks));
        var afterTicks = Math.Min(postTicks, maxTicks - beforeTicks);

        if (beforeTicks + afterTicks < maxTicks)
        {
            beforeTicks = Math.Min(preTicks, beforeTicks + (maxTicks - beforeTicks - afterTicks));
        }

        return (beforeTicks, afterTicks);
    }

    private static long SecondsToTicks(int seconds)
    {
        return TimeSpan.FromSeconds(seconds).Ticks;
    }
}
