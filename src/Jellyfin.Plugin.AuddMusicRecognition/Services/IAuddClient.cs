using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// AudD API client.
/// </summary>
public interface IAuddClient
{
    /// <summary>
    /// Sends a local audio clip to AudD.
    /// </summary>
    /// <param name="clipPath">Audio clip path.</param>
    /// <param name="apiToken">AudD API token.</param>
    /// <param name="returnMetadata">Optional metadata return list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The normalized recognition response.</returns>
    Task<RecognitionResponse> RecognizeAsync(
        string clipPath,
        string apiToken,
        string? returnMetadata,
        CancellationToken cancellationToken);
}
