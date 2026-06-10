using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Shazam recognition API client.
/// </summary>
public interface IShazamClient
{
    /// <summary>
    /// Sends a local audio clip to Shazam through RapidAPI.
    /// </summary>
    /// <param name="clipPath">Audio clip path.</param>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The normalized recognition response.</returns>
    Task<RecognitionResponse> RecognizeAsync(
        string clipPath,
        PluginConfiguration configuration,
        CancellationToken cancellationToken);
}
