using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-y");
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

        if (audioStreamIndex.HasValue)
        {
            startInfo.ArgumentList.Add("-map");
            startInfo.ArgumentList.Add(string.Create(CultureInfo.InvariantCulture, $"0:{audioStreamIndex.Value}"));
        }
        else
        {
            startInfo.ArgumentList.Add("-map");
            startInfo.ArgumentList.Add("0:a:0");
        }

        startInfo.ArgumentList.Add("-ac");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("-ar");
        startInfo.ArgumentList.Add("44100");
        startInfo.ArgumentList.Add("-b:a");
        startInfo.ArgumentList.Add("96k");
        startInfo.ArgumentList.Add(outputPath);

        try
        {
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
                TryDelete(outputPath);
                throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {stderr}");
            }

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                TryDelete(outputPath);
                throw new InvalidOperationException("FFmpeg did not produce an audio clip.");
            }

            return new ExtractedAudioClip(outputPath);
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
}
