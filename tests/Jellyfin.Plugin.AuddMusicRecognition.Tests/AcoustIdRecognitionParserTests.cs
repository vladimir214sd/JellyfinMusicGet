using System.Text.Json;
using Jellyfin.Plugin.AuddMusicRecognition.Services;
using Xunit;

namespace Jellyfin.Plugin.AuddMusicRecognition.Tests;

public sealed class AcoustIdRecognitionParserTests
{
    [Fact]
    public void Parse_ReturnsRecognizedResponse()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "status": "ok",
              "results": [
                {
                  "id": "9ff43b6a-4f16-427c-93c2-92307ca505e0",
                  "score": 0.98,
                  "recordings": [
                    {
                      "id": "cd2e7c47-16f5-46c6-a37c-a1eb7bf599ff",
                      "title": "Lower Your Eyelids to Die With the Sun",
                      "artists": [
                        { "id": "6d7b7cd4-254b-4c25-83f6-dd20f98ceacd", "name": "M83" }
                      ],
                      "releasegroups": [
                        { "id": "ddaa2d4d-314e-3e7c-b1d0-f6d207f5aa2f", "title": "Before the Dawn Heals Us", "type": "Album" }
                      ]
                    }
                  ]
                }
              ]
            }
            """);

        var response = AcoustIdRecognitionParser.Parse(document.RootElement, 0.65);

        Assert.Equal("recognized", response.Status);
        Assert.Equal("M83", response.Artist);
        Assert.Equal("Lower Your Eyelids to Die With the Sun", response.Title);
        Assert.Equal("Before the Dawn Heals Us", response.Album);
        Assert.Equal("https://acoustid.org/track/9ff43b6a-4f16-427c-93c2-92307ca505e0", response.SongLink);
        Assert.Equal(0.98, response.Confidence.GetValueOrDefault(), 3);
    }

    [Fact]
    public void Parse_ReturnsNoMatchWhenBestScoreIsBelowThreshold()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "status": "ok",
              "results": [
                {
                  "id": "9ff43b6a-4f16-427c-93c2-92307ca505e0",
                  "score": 0.42,
                  "recordings": [
                    { "title": "Angel", "artists": [{ "name": "Massive Attack" }] }
                  ]
                }
              ]
            }
            """);

        var response = AcoustIdRecognitionParser.Parse(document.RootElement, 0.65);

        Assert.Equal("no_match", response.Status);
        Assert.Equal("No match", response.StatusMessage);
    }

    [Fact]
    public void Parse_ReturnsErrorResponse()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "status": "error",
              "error": {
                "message": "invalid client API key"
              }
            }
            """);

        var response = AcoustIdRecognitionParser.Parse(document.RootElement, 0.65);

        Assert.Equal("error", response.Status);
        Assert.Equal("invalid client API key", response.StatusMessage);
    }
}
