using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using Jellyfin.Plugin.AuddMusicRecognition.Models;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// AcoustID recognition API client.
/// </summary>
public interface IAcoustIdClient
{
    /// <summary>
    /// Sends a local audio clip fingerprint to AcoustID.
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
