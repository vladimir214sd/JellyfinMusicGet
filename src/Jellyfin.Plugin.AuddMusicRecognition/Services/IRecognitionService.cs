using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using Jellyfin.Plugin.AuddMusicRecognition.Models;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Coordinates media extraction and AudD recognition.
/// </summary>
public interface IRecognitionService
{
    /// <summary>
    /// Recognizes music near a playback position.
    /// </summary>
    /// <param name="item">Jellyfin item.</param>
    /// <param name="request">Recognition request.</param>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recognition result.</returns>
    Task<RecognitionResponse> RecognizeAsync(
        BaseItem item,
        RecognitionRequest request,
        PluginConfiguration configuration,
        CancellationToken cancellationToken);
}
