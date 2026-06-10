namespace Jellyfin.Plugin.AuddMusicRecognition.Models;

/// <summary>
/// Payload passed by Jellyfin FileTransformation plugin.
/// </summary>
public sealed class PatchRequestPayload
{
    /// <summary>
    /// Gets or sets the file contents to transform.
    /// </summary>
    public string? Contents { get; set; }
}
