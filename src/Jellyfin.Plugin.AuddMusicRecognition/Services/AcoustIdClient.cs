using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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
        var endpoint = BuildLookupUri(configuration, fingerprint);

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return RecognitionResponse.Error($"AcoustID request failed with HTTP {(int)response.StatusCode}.");
        }

        return await AcoustIdRecognitionParser
            .ParseAsync(responseStream, configuration.AcoustIdMinimumScore, cancellationToken)
            .ConfigureAwait(false);
    }

    private static Uri BuildLookupUri(PluginConfiguration configuration, AudioFingerprint fingerprint)
    {
        var endpoint = NormalizeEndpoint(configuration.AcoustIdEndpoint);
        var duration = Math.Max(1, (int)Math.Round(fingerprint.DurationSeconds, MidpointRounding.AwayFromZero));
        var query = new[]
        {
            QueryParameter("format", "json"),
            QueryParameter("client", configuration.AcoustIdApiKey),
            QueryParameter("duration", duration.ToString(CultureInfo.InvariantCulture)),
            QueryParameter("fingerprint", fingerprint.Fingerprint),
            QueryParameter("meta", NormalizeMeta(configuration.AcoustIdMeta))
        };

        var builder = new UriBuilder(endpoint)
        {
            Query = string.Join("&", query)
        };

        return builder.Uri;
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
            return "recordings,releasegroups,compress";
        }

        return string.Join(
            ",",
            meta.Split([',', ' ', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string QueryParameter(string name, string value)
    {
        return $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
    }
}
