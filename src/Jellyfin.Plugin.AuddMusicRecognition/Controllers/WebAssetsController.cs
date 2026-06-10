using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.AuddMusicRecognition.Controllers;

/// <summary>
/// Serves web assets embedded in the plugin assembly.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("Plugins/AuddMusicRecognition/Web")]
public sealed class WebAssetsController : ControllerBase
{
    private const string OverlayScriptResource = "Jellyfin.Plugin.AuddMusicRecognition.Web.audd-music-recognition.plugin.js";

    /// <summary>
    /// Gets the Jellyfin Web player overlay script.
    /// </summary>
    /// <returns>JavaScript content.</returns>
    [HttpGet("audd-music-recognition.plugin.js")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult GetOverlayScript()
    {
        var assembly = typeof(WebAssetsController).Assembly;
        using var stream = assembly.GetManifestResourceStream(OverlayScriptResource);

        if (stream is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Content(reader.ReadToEnd(), "application/javascript; charset=utf-8", Encoding.UTF8);
    }
}
