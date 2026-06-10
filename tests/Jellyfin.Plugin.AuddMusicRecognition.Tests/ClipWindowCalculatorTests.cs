using System;
using Jellyfin.Plugin.AuddMusicRecognition.Services;
using Xunit;

namespace Jellyfin.Plugin.AuddMusicRecognition.Tests;

public sealed class ClipWindowCalculatorTests
{
    [Fact]
    public void Calculate_ShiftsForwardAtBeginning()
    {
        var window = ClipWindowCalculator.Calculate(
            Seconds(3),
            Seconds(100),
            preRollSeconds: 5,
            postRollSeconds: 8,
            maxClipSeconds: 15);

        Assert.Equal(Seconds(0), window.StartTicks);
        Assert.Equal(Seconds(13), window.EndTicks);
    }

    [Fact]
    public void Calculate_UsesConfiguredWindowInMiddle()
    {
        var window = ClipWindowCalculator.Calculate(
            Seconds(60),
            Seconds(100),
            preRollSeconds: 5,
            postRollSeconds: 8,
            maxClipSeconds: 15);

        Assert.Equal(Seconds(55), window.StartTicks);
        Assert.Equal(Seconds(68), window.EndTicks);
    }

    [Fact]
    public void Calculate_ShiftsBackwardAtEnd()
    {
        var window = ClipWindowCalculator.Calculate(
            Seconds(98),
            Seconds(100),
            preRollSeconds: 5,
            postRollSeconds: 8,
            maxClipSeconds: 15);

        Assert.Equal(Seconds(87), window.StartTicks);
        Assert.Equal(Seconds(100), window.EndTicks);
    }

    [Fact]
    public void Calculate_ClampsNegativePosition()
    {
        var window = ClipWindowCalculator.Calculate(
            Seconds(-4),
            Seconds(100),
            preRollSeconds: 5,
            postRollSeconds: 8,
            maxClipSeconds: 15);

        Assert.Equal(Seconds(0), window.StartTicks);
        Assert.Equal(Seconds(13), window.EndTicks);
    }

    [Fact]
    public void Calculate_ScalesWindowToMaximumLength()
    {
        var window = ClipWindowCalculator.Calculate(
            Seconds(50),
            Seconds(200),
            preRollSeconds: 20,
            postRollSeconds: 20,
            maxClipSeconds: 12);

        Assert.Equal(Seconds(44), window.StartTicks);
        Assert.Equal(Seconds(56), window.EndTicks);
    }

    private static long Seconds(int seconds)
    {
        return TimeSpan.FromSeconds(seconds).Ticks;
    }
}
