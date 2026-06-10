using System.Text.Json;
using Jellyfin.Plugin.AuddMusicRecognition.Services;
using Xunit;

namespace Jellyfin.Plugin.AuddMusicRecognition.Tests;

public sealed class AuddRecognitionParserTests
{
    [Fact]
    public void Parse_ReturnsRecognizedResponse()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "status": "success",
              "result": {
                "artist": "Massive Attack",
                "title": "Angel",
                "album": "Mezzanine",
                "timecode": "00:42",
                "song_link": "https://lis.tn/Angel",
                "spotify": {
                  "external_urls": {
                    "spotify": "https://open.spotify.com/track/example"
                  }
                },
                "apple_music": {
                  "url": "https://music.apple.com/example"
                }
              }
            }
            """);

        var response = AuddRecognitionParser.Parse(document.RootElement);

        Assert.Equal("recognized", response.Status);
        Assert.Equal("Massive Attack", response.Artist);
        Assert.Equal("Angel", response.Title);
        Assert.Equal("Mezzanine", response.Album);
        Assert.Equal("00:42", response.Timecode);
        Assert.Equal("https://lis.tn/Angel", response.SongLink);
        Assert.Equal("https://open.spotify.com/track/example", response.SpotifyUrl);
        Assert.Equal("https://music.apple.com/example", response.AppleMusicUrl);
    }

    [Fact]
    public void Parse_ReturnsNoMatchForEmptyArray()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "status": "success",
              "result": []
            }
            """);

        var response = AuddRecognitionParser.Parse(document.RootElement);

        Assert.Equal("no_match", response.Status);
        Assert.Equal("No match", response.StatusMessage);
    }

    [Fact]
    public void Parse_ReturnsErrorForInvalidToken()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "status": "error",
              "error_message": "Invalid API token"
            }
            """);

        var response = AuddRecognitionParser.Parse(document.RootElement);

        Assert.Equal("error", response.Status);
        Assert.Equal("Invalid API token", response.StatusMessage);
    }
}
