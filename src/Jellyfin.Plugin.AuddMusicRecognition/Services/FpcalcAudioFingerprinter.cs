using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Chromaprint fpcalc-backed audio fingerprinter.
/// </summary>
public sealed class FpcalcAudioFingerprinter : IAudioFingerprinter
{
    /// <inheritdoc />
    public async Task<AudioFingerprint> FingerprintAsync(
        string clipPath,
        string fpcalcPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clipPath) || !File.Exists(clipPath))
        {
            throw new FileNotFoundException("The audio clip is not available as a local file.", clipPath);
        }

        var executablePath = string.IsNullOrWhiteSpace(fpcalcPath) ? "fpcalc" : fpcalcPath;
        var startInfo = CreateStartInfo(executablePath, clipPath);

        try
        {
            var result = await RunAsync(startInfo, cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"fpcalc failed with exit code {result.ExitCode}: {result.Stderr.Trim()} | command: {result.Command}");
            }

            return ParseOutput(result.Stdout);
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException("fpcalc could not be started. Check the Chromaprint fpcalc path in plugin settings.", ex);
        }
    }

    private static ProcessStartInfo CreateStartInfo(string executablePath, string clipPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-json");
        startInfo.ArgumentList.Add(clipPath);

        return startInfo;
    }

    private static async Task<FpcalcRunResult> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        var command = FormatCommand(startInfo);
        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("fpcalc did not start.");
        }

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stderr = await stderrTask.ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);

        return new FpcalcRunResult(process.ExitCode, stdout, stderr, command);
    }

    private static AudioFingerprint ParseOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("fpcalc did not return a fingerprint.");
        }

        try
        {
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            var fingerprint = GetJsonString(root, "fingerprint");
            var duration = GetJsonDouble(root, "duration");

            return CreateFingerprint(fingerprint, duration);
        }
        catch (JsonException)
        {
            return ParseKeyValueOutput(output);
        }
    }

    private static AudioFingerprint ParseKeyValueOutput(string output)
    {
        string? fingerprint = null;
        double? duration = null;

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            if (string.Equals(parts[0], "FINGERPRINT", StringComparison.OrdinalIgnoreCase))
            {
                fingerprint = parts[1].Trim();
            }
            else if (string.Equals(parts[0], "DURATION", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDuration))
            {
                duration = parsedDuration;
            }
        }

        return CreateFingerprint(fingerprint, duration);
    }

    private static AudioFingerprint CreateFingerprint(string? fingerprint, double? duration)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new InvalidOperationException("fpcalc did not return a fingerprint.");
        }

        if (!duration.HasValue || duration.Value <= 0)
        {
            throw new InvalidOperationException("fpcalc did not return a valid duration.");
        }

        return new AudioFingerprint(fingerprint, duration.Value);
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static double? GetJsonDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var numberValue) => numberValue,
            JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var stringValue) => stringValue,
            _ => null
        };
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

    private sealed record FpcalcRunResult(int ExitCode, string Stdout, string Stderr, string Command);
}
