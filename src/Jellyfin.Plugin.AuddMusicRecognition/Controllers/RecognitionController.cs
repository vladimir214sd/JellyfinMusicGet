using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Models;
using Jellyfin.Plugin.AuddMusicRecognition.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.AuddMusicRecognition.Controllers;

/// <summary>
/// Recognition API consumed by the Jellyfin Web overlay.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/AuddMusicRecognition")]
public sealed class RecognitionController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly IRecognitionService _recognitionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecognitionController"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="recognitionService">Recognition service.</param>
    public RecognitionController(ILibraryManager libraryManager, IRecognitionService recognitionService)
    {
        _libraryManager = libraryManager;
        _recognitionService = recognitionService;
    }

    /// <summary>
    /// Recognizes the song playing near the requested item position.
    /// </summary>
    /// <param name="request">Recognition request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalized recognition response.</returns>
    [HttpPost("Recognize")]
    [ProducesResponseType(typeof(RecognitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RecognitionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecognitionResponse>> Recognize(
        [FromBody] RecognitionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ItemId == Guid.Empty)
        {
            return BadRequest(RecognitionResponse.Error("ItemId is required."));
        }

        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return BadRequest(RecognitionResponse.Error("Plugin is not initialized."));
        }

        var item = _libraryManager.GetItemById(request.ItemId);
        if (item is null)
        {
            return NotFound();
        }

        var result = await _recognitionService.RecognizeAsync(
            item,
            request,
            plugin.Configuration,
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }
}
