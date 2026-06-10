using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Parses Shazam/RapidAPI JSON responses into plugin DTOs.
/// </summary>
internal static class ShazamRecognitionParser
{
    /// <summary>
    /// Parses a Shazam response stream.
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
    /// Parses a Shazam response element.
    /// </summary>
    /// <param name="root">Root JSON element.</param>
    /// <returns>Normalized response.</returns>
    public static RecognitionResponse Parse(JsonElement root)
    {
        if (TryReadError(root, out var error))
        {
            return RecognitionResponse.Error(error);
        }

        var track = GetObject(root, "track") ?? GetNestedObject(root, "result", "track");
        if (!track.HasValue)
        {
            return RecognitionResponse.NoMatch();
        }

        var title = GetString(track.Value, "title");
        var artist = GetString(track.Value, "subtitle");

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
        {
            return RecognitionResponse.NoMatch();
        }

        return new RecognitionResponse
        {
            Status = "recognized",
            Artist = artist,
            Title = title,
            Album = GetAlbum(track.Value),
            SongLink = GetString(track.Value, "url") ?? GetNestedString(track.Value, "share", "href"),
            AppleMusicUrl = FindStringContaining(track.Value, "music.apple.com"),
            AlbumArtUrl = GetAlbumArtUrl(track.Value),
            StatusMessage = "Recognized"
        };
    }

    private static bool TryReadError(JsonElement root, out string message)
    {
        var status = GetString(root, "status");
        message = GetString(root, "message") ?? GetString(root, "error") ?? "Shazam returned an error.";

        return string.Equals(status, "error", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "failure", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetAlbum(JsonElement track)
    {
        if (!track.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var section in sections.EnumerateArray())
        {
            if (!section.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in metadata.EnumerateArray())
            {
                var title = GetString(item, "title");
                if (string.Equals(title, "Album", StringComparison.OrdinalIgnoreCase))
                {
                    return GetString(item, "text");
                }
            }
        }

        return null;
    }

    private static string? GetAlbumArtUrl(JsonElement track)
    {
        return GetNestedString(track, "images", "coverarthq")
            ?? GetNestedString(track, "images", "coverart")
            ?? GetNestedString(track, "share", "image")
            ?? GetNestedString(track, "images", "background");
    }

    private static JsonElement? GetObject(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : null;
    }

    private static JsonElement? GetNestedObject(JsonElement element, params string[] path)
    {
        var current = element;

        foreach (var part in path)
        {
            if (!current.TryGetProperty(part, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.Object ? current : null;
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

    private static string? FindStringContaining(JsonElement element, string value)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => Contains(element.GetString(), value) ? element.GetString() : null,
            JsonValueKind.Object => element.EnumerateObject()
                .Select(property => FindStringContaining(property.Value, value))
                .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => FindStringContaining(item, value))
                .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result)),
            _ => null
        };
    }

    private static bool Contains(string? source, string value)
    {
        return source?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
