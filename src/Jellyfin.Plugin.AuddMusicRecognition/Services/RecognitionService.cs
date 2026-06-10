using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using Jellyfin.Plugin.AuddMusicRecognition.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Coordinates clip extraction and music recognition.
/// </summary>
public sealed class RecognitionService : IRecognitionService
{
    private readonly IAudioClipExtractor _audioClipExtractor;
    private readonly IAuddClient _auddClient;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ILogger<RecognitionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecognitionService"/> class.
    /// </summary>
    /// <param name="audioClipExtractor">Audio clip extractor.</param>
    /// <param name="auddClient">AudD client.</param>
    /// <param name="mediaSourceManager">Jellyfin media source manager.</param>
    /// <param name="logger">Logger.</param>
    public RecognitionService(
        IAudioClipExtractor audioClipExtractor,
        IAuddClient auddClient,
        IMediaSourceManager mediaSourceManager,
        ILogger<RecognitionService> logger)
    {
        _audioClipExtractor = audioClipExtractor;
        _auddClient = auddClient;
        _mediaSourceManager = mediaSourceManager;
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

        var sourcePath = await ResolveLocalMediaPathAsync(item, request, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            _logger.LogWarning(
                "AudD path resolution failed. Item {ItemId}, media source {MediaSourceId}, request media source path supplied {HasMediaSourcePath}",
                item.Id,
                request.MediaSourceId,
                !string.IsNullOrWhiteSpace(request.MediaSourcePath));
            return RecognitionResponse.Error("Only local filesystem media can be recognized in v1. No readable local file path was found for the item or selected media source.");
        }

        var clipWindow = ClipWindowCalculator.Calculate(
            request.PositionTicks,
            item.RunTimeTicks,
            configuration.PreRollSeconds,
            configuration.PostRollSeconds,
            configuration.MaxClipSeconds);

        try
        {
            var response = await RecognizeClipAsync(
                item,
                sourcePath,
                clipWindow,
                request.AudioStreamIndex,
                configuration,
                cancellationToken).ConfigureAwait(false);

            if (string.Equals(response.Status, "no_match", StringComparison.OrdinalIgnoreCase)
                && request.AudioStreamIndex.HasValue)
            {
                _logger.LogInformation(
                    "AudD found no match for item {ItemId} using audio stream {AudioStreamIndex}; retrying with the first audio stream",
                    item.Id,
                    request.AudioStreamIndex);

                response = await RecognizeClipAsync(
                    item,
                    sourcePath,
                    clipWindow,
                    null,
                    configuration,
                    cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "AudD recognition failed for item {ItemId}",
                item.Id);
            return RecognitionResponse.Error(ex.Message);
        }
    }

    private async Task<RecognitionResponse> RecognizeClipAsync(
        BaseItem item,
        string sourcePath,
        ClipWindow clipWindow,
        int? audioStreamIndex,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Extracting music recognition clip for item {ItemId} from {SourcePath} at {StartSeconds}s for {DurationSeconds}s using audio stream {AudioStreamIndex}",
            item.Id,
            sourcePath,
            clipWindow.StartSeconds,
            clipWindow.DurationSeconds,
            audioStreamIndex);

        await using var clip = await _audioClipExtractor.ExtractAsync(
            sourcePath,
            clipWindow,
            audioStreamIndex,
            configuration.FfmpegPath,
            cancellationToken).ConfigureAwait(false);

        var clipSizeBytes = new FileInfo(clip.Path).Length;

        _logger.LogInformation(
            "Submitting AudD clip {ClipPath} for item {ItemId}; duration {DurationSeconds}s, size {ClipSizeBytes} bytes",
            clip.Path,
            item.Id,
            clipWindow.DurationSeconds,
            clipSizeBytes);

        var response = await _auddClient.RecognizeAsync(
            clip.Path,
            configuration.AuddApiToken,
            configuration.ReturnMetadata,
            cancellationToken).ConfigureAwait(false);

        response.ClipStartTicks = clipWindow.StartTicks;
        response.ClipDurationTicks = clipWindow.DurationTicks;
        response.ClipSizeBytes = clipSizeBytes;

        return response;
    }

    private async Task<string?> ResolveLocalMediaPathAsync(BaseItem item, RecognitionRequest request, CancellationToken cancellationToken)
    {
        var mediaSourceId = request.MediaSourceId;
        var candidates = new List<MediaPathCandidate>();
        AddCandidate(candidates, null, item.Path);

        foreach (var mediaSource in GetMediaSources(item))
        {
            AddCandidate(
                candidates,
                GetStringProperty(mediaSource, "Id"),
                GetStringProperty(mediaSource, "Path"));
        }

        AddMediaSourceCandidates(candidates, GetStaticMediaSources(item));

        foreach (var lookupMediaSourceId in GetMediaSourceLookupIds(item, mediaSourceId, candidates).ToList())
        {
            AddMediaSourceCandidate(
                candidates,
                await GetMediaSourceAsync(item, lookupMediaSourceId, cancellationToken).ConfigureAwait(false));
        }

        var selected = candidates
            .Where(candidate => IsMediaSourceMatch(candidate.MediaSourceId, mediaSourceId))
            .Select(candidate => candidate.Path)
            .FirstOrDefault(IsReadableLocalFile);

        var sourcePath = selected ?? candidates
            .Select(candidate => candidate.Path)
            .FirstOrDefault(IsReadableLocalFile);

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            var requestPath = NormalizeLocalPath(request.MediaSourcePath);
            if (IsReadableLocalFile(requestPath))
            {
                sourcePath = requestPath;
                _logger.LogDebug(
                    "Using client-supplied media path as a final fallback for item {ItemId}: {SourcePath}",
                    item.Id,
                    sourcePath);
            }
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            _logger.LogWarning(
                "No readable local media path found for item {ItemId}. Item type: {ItemType}; item path: {ItemPath}; requested media source: {MediaSourceId}; candidates: {Candidates}",
                item.Id,
                item.GetType().FullName,
                item.Path,
                mediaSourceId,
                string.Join("; ", candidates.Select(FormatCandidateForLog)));
        }

        return sourcePath;
    }

    private IReadOnlyList<MediaSourceInfo> GetStaticMediaSources(BaseItem item)
    {
        try
        {
            return _mediaSourceManager.GetStaticMediaSources(item, false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read static media sources for item {ItemId}", item.Id);
            return [];
        }
    }

    private async Task<MediaSourceInfo?> GetMediaSourceAsync(BaseItem item, string mediaSourceId, CancellationToken cancellationToken)
    {
        try
        {
            return await _mediaSourceManager
                .GetMediaSource(item, mediaSourceId, null!, false, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Could not read selected media source {MediaSourceId} for item {ItemId}",
                mediaSourceId,
                item.Id);
            return null;
        }
    }

    private static void AddMediaSourceCandidates(ICollection<MediaPathCandidate> candidates, IEnumerable<MediaSourceInfo> mediaSources)
    {
        foreach (var mediaSource in mediaSources)
        {
            AddMediaSourceCandidate(candidates, mediaSource);
        }
    }

    private static void AddMediaSourceCandidate(ICollection<MediaPathCandidate> candidates, MediaSourceInfo? mediaSource)
    {
        if (mediaSource is not null)
        {
            AddCandidate(candidates, mediaSource.Id, mediaSource.Path);
        }
    }

    private static IEnumerable<string> GetMediaSourceLookupIds(
        BaseItem item,
        string? requestedMediaSourceId,
        IEnumerable<MediaPathCandidate> candidates)
    {
        var ids = new[]
        {
            requestedMediaSourceId,
            item.Id.ToString("N"),
            item.Id.ToString("D")
        }.Concat(candidates.Select(candidate => candidate.MediaSourceId));

        return ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<object> GetMediaSources(BaseItem item)
    {
        foreach (var source in GetMediaSourcesFromProperties(item))
        {
            yield return source;
        }

        foreach (var source in GetMediaSourcesFromMethods(item))
        {
            yield return source;
        }
    }

    private static IEnumerable<object> GetMediaSourcesFromProperties(BaseItem item)
    {
        foreach (var propertyName in new[] { "MediaSources", "AlternateSources" })
        {
            var property = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            foreach (var source in EnumerateObjects(property?.GetValue(item)))
            {
                yield return source;
            }
        }
    }

    private static IEnumerable<object> GetMediaSourcesFromMethods(BaseItem item)
    {
        var methods = item.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => string.Equals(method.Name, "GetMediaSources", StringComparison.Ordinal))
            .OrderBy(method => method.GetParameters().Length);

        foreach (var method in methods)
        {
            object? result = null;
            var parameters = method.GetParameters();

            try
            {
                result = parameters.Length switch
                {
                    0 => method.Invoke(item, null),
                    1 when parameters[0].ParameterType == typeof(bool) => method.Invoke(item, [false]),
                    _ => null
                };
            }
            catch (TargetInvocationException)
            {
            }
            catch (ArgumentException)
            {
            }

            foreach (var source in EnumerateObjects(result))
            {
                yield return source;
            }
        }
    }

    private static IEnumerable<object> EnumerateObjects(object? value)
    {
        if (value is null || value is string)
        {
            yield break;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    yield return item;
                }
            }
        }
        else
        {
            yield return value;
        }
    }

    private static void AddCandidate(ICollection<MediaPathCandidate> candidates, string? mediaSourceId, string? path)
    {
        var localPath = NormalizeLocalPath(path);
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            candidates.Add(new MediaPathCandidate(mediaSourceId, localPath));
        }
    }

    private static string? NormalizeLocalPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var localPath = path.Trim().Trim('"', '\'');
        if (localPath.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            var filePath = localPath.Substring("file:".Length).Trim().Trim('"', '\'');
            if (filePath.StartsWith("//", StringComparison.Ordinal) || filePath.StartsWith(@"\\", StringComparison.Ordinal))
            {
                localPath = "file:" + filePath;
            }
            else if (!string.IsNullOrWhiteSpace(filePath))
            {
                localPath = filePath;
            }
        }

        if (Uri.TryCreate(localPath, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return localPath;
    }

    private static string? GetStringProperty(object source, string propertyName)
    {
        return source
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(source)?
            .ToString();
    }

    private static bool IsMediaSourceMatch(string? candidateMediaSourceId, string? requestedMediaSourceId)
    {
        return string.IsNullOrWhiteSpace(requestedMediaSourceId)
            || string.Equals(candidateMediaSourceId, requestedMediaSourceId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReadableLocalFile(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    private static string FormatCandidateForLog(MediaPathCandidate candidate)
    {
        return $"source={candidate.MediaSourceId ?? "<none>"}, path={candidate.Path}, exists={File.Exists(candidate.Path)}";
    }

    private sealed record MediaPathCandidate(string? MediaSourceId, string Path);
}
