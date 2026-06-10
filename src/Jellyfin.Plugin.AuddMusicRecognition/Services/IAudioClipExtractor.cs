using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Extracts short audio clips from media files.
/// </summary>
public interface IAudioClipExtractor
{
    /// <summary>
    /// Extracts a clip as a temporary MP3 file.
    /// </summary>
    /// <param name="sourcePath">Source media path.</param>
    /// <param name="window">Clip window.</param>
    /// <param name="audioStreamIndex">Optional audio stream index.</param>
    /// <param name="ffmpegPath">FFmpeg executable path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The temporary extracted clip.</returns>
    Task<ExtractedAudioClip> ExtractAsync(
        string sourcePath,
        ClipWindow window,
        int? audioStreamIndex,
        string ffmpegPath,
        CancellationToken cancellationToken);
}
