using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using Jellyfin.Plugin.AuddMusicRecognition.Models;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Coordinates clip extraction and AudD recognition.
/// </summary>
public sealed class RecognitionService : IRecognitionService
{
    private readonly IAudioClipExtractor _audioClipExtractor;
    private readonly IAuddClient _auddClient;
    private readonly ILogger<RecognitionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecognitionService"/> class.
    /// </summary>
    /// <param name="audioClipExtractor">Audio clip extractor.</param>
    /// <param name="auddClient">AudD client.</param>
    /// <param name="logger">Logger.</param>
    public RecognitionService(
        IAudioClipExtractor audioClipExtractor,
        IAuddClient auddClient,
        ILogger<RecognitionService> logger)
    {
        _audioClipExtractor = audioClipExtractor;
        _auddClient = auddClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RecognitionResponse> RecognizeAsync(
        BaseItem item,
        RecognitionRequest request,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.AuddApiToken))
        {
            return RecognitionResponse.Error("AudD API token is not configured.");
        }

        if (string.IsNullOrWhiteSpace(item.Path) || !File.Exists(item.Path))
        {
            return RecognitionResponse.Error("Only local filesystem media can be recognized in v1.");
        }

        var clipWindow = ClipWindowCalculator.Calculate(
            request.PositionTicks,
            item.RunTimeTicks,
            configuration.PreRollSeconds,
            configuration.PostRollSeconds,
            configuration.MaxClipSeconds);

        try
        {
            _logger.LogInformation(
                "Extracting AudD clip for item {ItemId} from {SourcePath} at {StartSeconds}s for {DurationSeconds}s using audio stream {AudioStreamIndex}",
                item.Id,
                item.Path,
                clipWindow.StartSeconds,
                clipWindow.DurationSeconds,
                request.AudioStreamIndex);

            await using var clip = await _audioClipExtractor.ExtractAsync(
                item.Path,
                clipWindow,
                request.AudioStreamIndex,
                configuration.FfmpegPath,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Submitting AudD clip {ClipPath} for item {ItemId}", clip.Path, item.Id);

            var response = await _auddClient.RecognizeAsync(
                clip.Path,
                configuration.AuddApiToken,
                configuration.ReturnMetadata,
                cancellationToken).ConfigureAwait(false);

            response.ClipStartTicks = clipWindow.StartTicks;
            response.ClipDurationTicks = clipWindow.DurationTicks;

            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Music recognition failed for item {ItemId}", item.Id);
            return RecognitionResponse.Error(ex.Message);
        }
    }
}
