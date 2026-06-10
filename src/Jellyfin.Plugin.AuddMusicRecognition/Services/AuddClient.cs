using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// HTTP client for AudD recognition.
/// </summary>
public sealed class AuddClient : IAuddClient
{
    private static readonly Uri Endpoint = new("https://api.audd.io/");

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuddClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    public AuddClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<RecognitionResponse> RecognizeAsync(
        string clipPath,
        string apiToken,
        string? returnMetadata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return RecognitionResponse.Error("AudD API token is not configured.");
        }

        await using var fileStream = File.OpenRead(clipPath);
        using var content = new MultipartFormDataContent();
        using var tokenContent = new StringContent(apiToken);
        using var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        content.Add(tokenContent, "api_token");
        content.Add(fileContent, "file", Path.GetFileName(clipPath));

        if (!string.IsNullOrWhiteSpace(returnMetadata))
        {
            content.Add(new StringContent(returnMetadata), "return");
        }

        using var response = await _httpClient.PostAsync(Endpoint, content, cancellationToken).ConfigureAwait(false);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return RecognitionResponse.Error($"AudD request failed with HTTP {(int)response.StatusCode}.");
        }

        return await AuddRecognitionParser.ParseAsync(responseStream, cancellationToken).ConfigureAwait(false);
    }
}
