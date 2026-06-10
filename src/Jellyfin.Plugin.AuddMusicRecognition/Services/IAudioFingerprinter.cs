using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Produces audio fingerprints for fingerprint-based providers.
/// </summary>
public interface IAudioFingerprinter
{
    /// <summary>
    /// Generates a fingerprint for a local audio clip.
    /// </summary>
    /// <param name="clipPath">Audio clip path.</param>
    /// <param name="fpcalcPath">Chromaprint fpcalc executable path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audio fingerprint.</returns>
    Task<AudioFingerprint> FingerprintAsync(
        string clipPath,
        string fpcalcPath,
        CancellationToken cancellationToken);
}
