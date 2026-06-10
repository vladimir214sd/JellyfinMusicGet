using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Parses AcoustID JSON responses into plugin DTOs.
/// </summary>
internal static class AcoustIdRecognitionParser
{
    /// <summary>
    /// Parses an AcoustID response stream.
    /// </summary>
    /// <param name="stream">Response stream.</param>
    /// <param name="minimumScore">Minimum accepted match score.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalized response.</returns>
    public static async Task<RecognitionResponse> ParseAsync(
        Stream stream,
        double minimumScore,
        CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Parse(document.RootElement, minimumScore);
    }

    /// <summary>
    /// Parses an AcoustID response element.
    /// </summary>
    /// <param name="root">Root JSON element.</param>
    /// <param name="minimumScore">Minimum accepted match score.</param>
    /// <returns>Normalized response.</returns>
    public static RecognitionResponse Parse(JsonElement root, double minimumScore)
    {
        if (TryReadError(root, out var error))
        {
            return RecognitionResponse.Error(error);
        }

        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return RecognitionResponse.NoMatch();
        }

        var threshold = NormalizeMinimumScore(minimumScore);
        var best = GetBestResult(results);
        if (!best.Result.HasValue || best.Score < threshold)
        {
            return RecognitionResponse.NoMatch();
        }

        var recording = GetBestRecording(best.Result.Value);
        if (!recording.HasValue)
        {
            return RecognitionResponse.NoMatch();
        }

        var title = GetString(recording.Value, "title");
        var artist = GetArtists(recording.Value);
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
        {
            return RecognitionResponse.NoMatch();
        }

        var acoustId = GetString(best.Result.Value, "id");

        return new RecognitionResponse
        {
            Status = "recognized",
            Artist = artist,
            Title = title,
            Album = GetAlbum(recording.Value),
            SongLink = string.IsNullOrWhiteSpace(acoustId) ? null : $"https://acoustid.org/track/{Uri.EscapeDataString(acoustId)}",
            Confidence = best.Score,
            StatusMessage = "Recognized"
        };
    }

    private static bool TryReadError(JsonElement root, out string message)
    {
        var status = GetString(root, "status");
        message = GetNestedString(root, "error", "message")
            ?? GetString(root, "error")
            ?? GetString(root, "message")
            ?? "AcoustID returned an error.";

        return string.Equals(status, "error", StringComparison.OrdinalIgnoreCase);
    }

    private static (JsonElement? Result, double Score) GetBestResult(JsonElement results)
    {
        JsonElement? bestResult = null;
        var bestScore = double.MinValue;

        foreach (var result in results.EnumerateArray())
        {
            if (result.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var score = GetDouble(result, "score") ?? 0;
            if (score > bestScore)
            {
                bestScore = score;
                bestResult = result;
            }
        }

        return (bestResult, bestScore);
    }

    private static JsonElement? GetBestRecording(JsonElement result)
    {
        if (!result.TryGetProperty("recordings", out var recordings) || recordings.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        JsonElement? firstRecording = null;
        foreach (var recording in recordings.EnumerateArray())
        {
            if (recording.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            firstRecording ??= recording;
            if (!string.IsNullOrWhiteSpace(GetString(recording, "title"))
                && !string.IsNullOrWhiteSpace(GetArtists(recording)))
            {
                return recording;
            }
        }

        return firstRecording;
    }

    private static string? GetArtists(JsonElement recording)
    {
        if (!recording.TryGetProperty("artists", out var artists) || artists.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var names = artists
            .EnumerateArray()
            .Where(artist => artist.ValueKind == JsonValueKind.Object)
            .Select(artist => GetString(artist, "name"))
            .Where(name => !string.IsNullOrWhiteSpace(name));

        return string.Join(", ", names);
    }

    private static string? GetAlbum(JsonElement recording)
    {
        if (!recording.TryGetProperty("releasegroups", out var releaseGroups) || releaseGroups.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var releaseGroup in releaseGroups.EnumerateArray())
        {
            if (releaseGroup.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var title = GetString(releaseGroup, "title");
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static string? GetNestedString(JsonElement element, params string[] path)
    {
        var current = element;

        foreach (var part in path)
        {
            if (!current.TryGetProperty(part, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static double? GetDouble(JsonElement element, string propertyName)
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

    private static double NormalizeMinimumScore(double minimumScore)
    {
        if (double.IsNaN(minimumScore) || double.IsInfinity(minimumScore))
        {
            return 0.65;
        }

        return Math.Clamp(minimumScore, 0, 1);
    }
}
