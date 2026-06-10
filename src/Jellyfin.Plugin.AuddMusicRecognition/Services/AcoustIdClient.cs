using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// HTTP client for AcoustID recognition.
/// </summary>
public sealed class AcoustIdClient : IAcoustIdClient
{
    private static readonly Uri DefaultEndpoint = new("https://api.acoustid.org/v2/lookup");

    private readonly HttpClient _httpClient;
    private readonly IAudioFingerprinter _audioFingerprinter;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcoustIdClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="audioFingerprinter">Audio fingerprinter.</param>
    public AcoustIdClient(HttpClient httpClient, IAudioFingerprinter audioFingerprinter)
    {
        _httpClient = httpClient;
        _audioFingerprinter = audioFingerprinter;
    }

    /// <inheritdoc />
    public async Task<RecognitionResponse> RecognizeAsync(
        string clipPath,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.AcoustIdApiKey))
        {
            return RecognitionResponse.Error("AcoustID API key is not configured.");
        }

        var fingerprint = await _audioFingerprinter
            .FingerprintAsync(clipPath, configuration.FpcalcPath, cancellationToken)
            .ConfigureAwait(false);
        var endpoint = NormalizeEndpoint(configuration.AcoustIdEndpoint);
        using var content = BuildLookupContent(configuration, fingerprint);

        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = GetErrorMessage(responseBody);
            return RecognitionResponse.Error(
                string.IsNullOrWhiteSpace(error)
                    ? $"AcoustID request failed with HTTP {(int)response.StatusCode}."
                    : $"AcoustID request failed with HTTP {(int)response.StatusCode}: {error}");
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return AcoustIdRecognitionParser.Parse(document.RootElement, configuration.AcoustIdMinimumScore);
        }
        catch (JsonException)
        {
            return RecognitionResponse.Error("AcoustID returned an invalid JSON response.");
        }
    }

    private static FormUrlEncodedContent BuildLookupContent(
        PluginConfiguration configuration,
        AudioFingerprint fingerprint)
    {
        var duration = Math.Max(1, (int)Math.Round(fingerprint.DurationSeconds, MidpointRounding.AwayFromZero));
        return new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("format", "json"),
            new KeyValuePair<string, string>("client", configuration.AcoustIdApiKey),
            new KeyValuePair<string, string>("duration", duration.ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string>("fingerprint", fingerprint.Fingerprint),
            new KeyValuePair<string, string>("meta", NormalizeMeta(configuration.AcoustIdMeta))
        ]);
    }

    private static string? GetErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? message.ToString();
                }

                return error.ValueKind == JsonValueKind.String ? error.GetString() : error.ToString();
            }

            return root.TryGetProperty("message", out var rootMessage)
                ? rootMessage.GetString() ?? rootMessage.ToString()
                : null;
        }
        catch (JsonException)
        {
            var compact = responseBody.Trim();
            return compact.Length <= 300 ? compact : compact[..300];
        }
    }

    private static Uri NormalizeEndpoint(string? endpoint)
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host)
            ? uri
            : DefaultEndpoint;
    }

    private static string NormalizeMeta(string? meta)
    {
        if (string.IsNullOrWhiteSpace(meta))
        {
            return "recordings releasegroups compress";
        }

        return string.Join(
            " ",
            meta.Split([',', ' ', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
