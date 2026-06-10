namespace Jellyfin.Plugin.AuddMusicRecognition.Models;

/// <summary>
/// Normalized recognition response returned to Jellyfin Web.
/// </summary>
public sealed class RecognitionResponse
{
    /// <summary>
    /// Gets or sets the normalized status: recognized, no_match, or error.
    /// </summary>
    public string Status { get; set; } = "error";

    /// <summary>
    /// Gets or sets the recognized artist.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Gets or sets the recognized title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the recognized album.
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Gets or sets the AudD timecode inside the recognized song.
    /// </summary>
    public string? Timecode { get; set; }

    /// <summary>
    /// Gets or sets the AudD song link.
    /// </summary>
    public string? SongLink { get; set; }

    /// <summary>
    /// Gets or sets the Spotify track URL when returned by AudD.
    /// </summary>
    public string? SpotifyUrl { get; set; }

    /// <summary>
    /// Gets or sets the Apple Music URL when returned by AudD.
    /// </summary>
    public string? AppleMusicUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional confidence value, when a provider supplies one.
    /// </summary>
    public double? Confidence { get; set; }

    /// <summary>
    /// Gets or sets a display-safe status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the extracted clip start position in ticks.
    /// </summary>
    public long? ClipStartTicks { get; set; }

    /// <summary>
    /// Gets or sets the extracted clip duration in ticks.
    /// </summary>
    public long? ClipDurationTicks { get; set; }

    /// <summary>
    /// Creates a no-match response.
    /// </summary>
    /// <returns>The response.</returns>
    public static RecognitionResponse NoMatch()
    {
        return new RecognitionResponse
        {
            Status = "no_match",
            StatusMessage = "No match"
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The response.</returns>
    public static RecognitionResponse Error(string message)
    {
        return new RecognitionResponse
        {
            Status = "error",
            StatusMessage = message
        };
    }
}
