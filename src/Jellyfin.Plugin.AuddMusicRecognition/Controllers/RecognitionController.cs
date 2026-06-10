using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Models;
using Jellyfin.Plugin.AuddMusicRecognition.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<RecognitionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecognitionController"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="recognitionService">Recognition service.</param>
    /// <param name="logger">Logger.</param>
    public RecognitionController(
        ILibraryManager libraryManager,
        IRecognitionService recognitionService,
        ILogger<RecognitionController> logger)
    {
        _libraryManager = libraryManager;
        _recognitionService = recognitionService;
        _logger = logger;
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
            _logger.LogWarning("Music recognition request rejected because item id is empty.");
            return BadRequest(RecognitionResponse.Error("ItemId is required."));
        }

        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            _logger.LogWarning("Music recognition request rejected because plugin instance is not initialized.");
            return BadRequest(RecognitionResponse.Error("Plugin is not initialized."));
        }

        var provider = plugin.Configuration.RecognitionProvider;

        _logger.LogInformation(
            "Music recognition request received for item {ItemId} with provider {RecognitionProvider}, media source {MediaSourceId}, playback info path supplied {HasMediaSourcePath}, position {PositionTicks}, audio stream {AudioStreamIndex}",
            request.ItemId,
            provider,
            request.MediaSourceId,
            !string.IsNullOrWhiteSpace(request.MediaSourcePath),
            request.PositionTicks,
            request.AudioStreamIndex);

        var item = _libraryManager.GetItemById(request.ItemId);
        if (item is null)
        {
            _logger.LogWarning("Music recognition request item was not found: {ItemId}", request.ItemId);
            return NotFound();
        }

        var result = await _recognitionService.RecognizeAsync(
            item,
            request,
            plugin.Configuration,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Music recognition completed for item {ItemId} with provider {RecognitionProvider}, status {Status}: {StatusMessage}",
            request.ItemId,
            provider,
            result.Status,
            result.StatusMessage);

        return Ok(result);
    }
}
