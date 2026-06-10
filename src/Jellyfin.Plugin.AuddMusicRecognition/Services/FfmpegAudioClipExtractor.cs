using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// FFmpeg-backed audio clip extractor.
/// </summary>
public sealed class FfmpegAudioClipExtractor : IAudioClipExtractor
{
    /// <inheritdoc />
    public async Task<ExtractedAudioClip> ExtractAsync(
        string sourcePath,
        ClipWindow window,
        int? audioStreamIndex,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The media item is not available as a local file.", sourcePath);
        }

        var executablePath = string.IsNullOrWhiteSpace(ffmpegPath) ? "ffmpeg" : ffmpegPath;
        var outputPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            string.Create(CultureInfo.InvariantCulture, $"jellyfin-audd-{Guid.NewGuid():N}.mp3"));

        var failures = new List<string>();

        try
        {
            foreach (var map in GetMapCandidates(audioStreamIndex))
            {
                TryDelete(outputPath);
                var startInfo = CreateStartInfo(executablePath, sourcePath, outputPath, window, map);
                var result = await RunAsync(startInfo, outputPath, cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    return new ExtractedAudioClip(outputPath);
                }

                failures.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"map {DescribeMap(map)} failed with exit code {result.ExitCode}: {result.Stderr.Trim()} | command: {result.Command}"));
            }
        }
        catch (Win32Exception ex)
        {
            TryDelete(outputPath);
            throw new InvalidOperationException("FFmpeg could not be started. Check the FFmpeg path in plugin settings.", ex);
        }
        catch
        {
            TryDelete(outputPath);
            throw;
        }

        TryDelete(outputPath);
        throw new InvalidOperationException($"FFmpeg failed to extract an audio clip. {string.Join(" || ", failures)}");
    }

    private static ProcessStartInfo CreateStartInfo(
        string executablePath,
        string sourcePath,
        string outputPath,
        ClipWindow window,
        string? map)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-nostdin");
        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-loglevel");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(window.StartSeconds);
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add("-t");
        startInfo.ArgumentList.Add(window.DurationSeconds);
        startInfo.ArgumentList.Add("-vn");
        startInfo.ArgumentList.Add("-sn");
        startInfo.ArgumentList.Add("-dn");

        if (!string.IsNullOrWhiteSpace(map))
        {
            startInfo.ArgumentList.Add("-map");
            startInfo.ArgumentList.Add(map);
        }

        startInfo.ArgumentList.Add("-ac");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("-ar");
        startInfo.ArgumentList.Add("44100");
        startInfo.ArgumentList.Add("-b:a");
        startInfo.ArgumentList.Add("96k");
        startInfo.ArgumentList.Add(outputPath);

        return startInfo;
    }

    private static async Task<FfmpegRunResult> RunAsync(ProcessStartInfo startInfo, string outputPath, CancellationToken cancellationToken)
    {
        var command = FormatCommand(startInfo);
        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("FFmpeg did not start.");
        }

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stderr = await stderrTask.ConfigureAwait(false);
        _ = await stdoutTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            return new FfmpegRunResult(false, process.ExitCode, stderr, command);
        }

        if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
        {
            return new FfmpegRunResult(false, process.ExitCode, "FFmpeg did not produce an audio clip.", command);
        }

        return new FfmpegRunResult(true, process.ExitCode, stderr, command);
    }

    private static IEnumerable<string?> GetMapCandidates(int? audioStreamIndex)
    {
        var candidates = new List<string?>();

        if (audioStreamIndex.HasValue && audioStreamIndex.Value >= 0)
        {
            candidates.Add(string.Create(CultureInfo.InvariantCulture, $"0:{audioStreamIndex.Value}"));
            candidates.Add(string.Create(CultureInfo.InvariantCulture, $"0:a:{audioStreamIndex.Value}"));
        }

        candidates.Add("0:a:0");
        candidates.Add(null);

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string DescribeMap(string? map)
    {
        return string.IsNullOrWhiteSpace(map) ? "auto" : map;
    }

    private static string FormatCommand(ProcessStartInfo startInfo)
    {
        return string.Join(
            " ",
            new[] { startInfo.FileName }.Concat(startInfo.ArgumentList.Select(QuoteArgument)));
    }

    private static string QuoteArgument(string argument)
    {
        return argument.Contains(' ', StringComparison.Ordinal) || argument.Contains('"', StringComparison.Ordinal)
            ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : argument;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record FfmpegRunResult(bool Success, int ExitCode, string Stderr, string Command);
}
