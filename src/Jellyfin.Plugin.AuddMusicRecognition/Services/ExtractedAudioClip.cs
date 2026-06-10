using System;
using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Temporary extracted audio clip.
/// </summary>
public sealed class ExtractedAudioClip : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractedAudioClip"/> class.
    /// </summary>
    /// <param name="path">Temporary file path.</param>
    public ExtractedAudioClip(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Gets the temporary file path.
    /// </summary>
    public string Path { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        TryDelete();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TryDelete();
        return ValueTask.CompletedTask;
    }

    private void TryDelete()
    {
        try
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
