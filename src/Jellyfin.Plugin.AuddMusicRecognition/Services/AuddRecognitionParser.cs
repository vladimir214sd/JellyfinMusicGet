using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Parses AudD JSON responses into plugin DTOs.
/// </summary>
internal static class AuddRecognitionParser
{
    /// <summary>
    /// Parses an AudD response stream.
    /// </summary>
    /// <param name="stream">Response stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalized response.</returns>
    public static async Task<RecognitionResponse> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Parse(document.RootElement);
    }

    /// <summary>
    /// Parses an AudD response element.
    /// </summary>
    /// <param name="root">Root JSON element.</param>
    /// <returns>Normalized response.</returns>
    public static RecognitionResponse Parse(JsonElement root)
    {
        var status = GetString(root, "status");

        if (string.Equals(status, "error", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "failure", System.StringComparison.OrdinalIgnoreCase))
        {
            return RecognitionResponse.Error(GetString(root, "error_message") ?? "AudD returned an error.");
        }

        if (!root.TryGetProperty("result", out var result)
            || result.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return RecognitionResponse.NoMatch();
        }

        var match = SelectMatch(result);
        if (match.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return RecognitionResponse.NoMatch();
        }

        var artist = GetString(match, "artist");
        var title = GetString(match, "title");

        if (string.IsNullOrWhiteSpace(artist) && string.IsNullOrWhiteSpace(title))
        {
            return RecognitionResponse.NoMatch();
        }

        return new RecognitionResponse
        {
            Status = "recognized",
            Artist = artist,
            Title = title,
            Album = GetString(match, "album"),
            Timecode = GetString(match, "timecode"),
            SongLink = GetString(match, "song_link"),
            SpotifyUrl = GetNestedString(match, "spotify", "external_urls", "spotify"),
            AppleMusicUrl = GetNestedString(match, "apple_music", "url"),
            Confidence = GetDouble(match, "confidence"),
            StatusMessage = "Recognized"
        };
    }

    private static JsonElement SelectMatch(JsonElement result)
    {
        return result.ValueKind switch
        {
            JsonValueKind.Object => result,
            JsonValueKind.Array => result.EnumerateArray().FirstOrDefault(),
            _ => default
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.TryGetDouble(out var value) ? value : null;
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
}
