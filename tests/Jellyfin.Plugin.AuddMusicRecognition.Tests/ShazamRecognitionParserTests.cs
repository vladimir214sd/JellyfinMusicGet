using System.Text.Json;
using Jellyfin.Plugin.AuddMusicRecognition.Services;
using Xunit;

namespace Jellyfin.Plugin.AuddMusicRecognition.Tests;

public sealed class ShazamRecognitionParserTests
{
    [Fact]
    public void Parse_ReturnsRecognizedResponse()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "matches": [{ "id": "123" }],
              "track": {
                "title": "Nightcall",
                "subtitle": "Kavinsky",
                "url": "https://www.shazam.com/track/123/nightcall",
                "images": {
                  "coverarthq": "https://is1-ssl.mzstatic.com/image/thumb/cover.jpg"
                },
                "hub": {
                  "options": [
                    {
                      "actions": [
                        {
                          "uri": "https://music.apple.com/us/album/nightcall/123?i=456"
                        }
                      ]
                    }
                  ]
                },
                "sections": [
                  {
                    "type": "SONG",
                    "metadata": [
                      { "title": "Album", "text": "Nightcall" }
                    ]
                  }
                ]
              }
            }
            """);

        var response = ShazamRecognitionParser.Parse(document.RootElement);

        Assert.Equal("recognized", response.Status);
        Assert.Equal("Kavinsky", response.Artist);
        Assert.Equal("Nightcall", response.Title);
        Assert.Equal("Nightcall", response.Album);
        Assert.Equal("https://www.shazam.com/track/123/nightcall", response.SongLink);
        Assert.Equal("https://music.apple.com/us/album/nightcall/123?i=456", response.AppleMusicUrl);
        Assert.Equal("https://is1-ssl.mzstatic.com/image/thumb/cover.jpg", response.AlbumArtUrl);
    }

    [Fact]
    public void Parse_ReturnsNoMatchWithoutTrack()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "matches": []
            }
            """);

        var response = ShazamRecognitionParser.Parse(document.RootElement);

        Assert.Equal("no_match", response.Status);
        Assert.Equal("No match", response.StatusMessage);
    }
}
