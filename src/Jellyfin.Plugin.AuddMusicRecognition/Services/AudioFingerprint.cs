namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Audio fingerprint data produced by Chromaprint.
/// </summary>
/// <param name="Fingerprint">Chromaprint fingerprint.</param>
/// <param name="DurationSeconds">Fingerprint duration in seconds.</param>
public sealed record AudioFingerprint(string Fingerprint, double DurationSeconds);
