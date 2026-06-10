using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// HTTP client for Shazam recognition through RapidAPI.
/// </summary>
public sealed class ShazamRapidApiClient : IShazamClient
{
    private const string DefaultHost = "shazam.p.rapidapi.com";
    private const string DetectPath = "/songs/v2/detect";

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShazamRapidApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    public ShazamRapidApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<RecognitionResponse> RecognizeAsync(
        string clipPath,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.ShazamRapidApiKey))
        {
            return RecognitionResponse.Error("Shazam RapidAPI key is not configured.");
        }

        var host = NormalizeHost(configuration.ShazamRapidApiHost);
        var endpoint = BuildEndpoint(host, configuration.ShazamLocale, configuration.ShazamTimeZone);
        var audioBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(clipPath, cancellationToken).ConfigureAwait(false));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("X-RapidAPI-Key", configuration.ShazamRapidApiKey);
        request.Headers.TryAddWithoutValidation("X-RapidAPI-Host", host);
        request.Content = new StringContent(audioBase64, Encoding.UTF8, "text/plain");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return RecognitionResponse.Error($"Shazam request failed with HTTP {(int)response.StatusCode}.");
        }

        return await ShazamRecognitionParser.ParseAsync(responseStream, cancellationToken).ConfigureAwait(false);
    }

    private static Uri BuildEndpoint(string host, string? locale, string? timezone)
    {
        var builder = new UriBuilder(Uri.UriSchemeHttps, host)
        {
            Path = DetectPath.TrimStart('/'),
            Query = string.Join(
                "&",
                new[]
                {
                $"locale={Uri.EscapeDataString(string.IsNullOrWhiteSpace(locale) ? "en-US" : locale)}",
                $"timezone={Uri.EscapeDataString(string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone)}"
                })
        };

        return builder.Uri;
    }

    private static string NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return DefaultHost;
        }

        var trimmed = host.Trim().TrimEnd('/');
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host)
            ? uri.Host
            : trimmed.Split('/')[0];
    }
}
