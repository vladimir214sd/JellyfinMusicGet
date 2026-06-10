using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.AuddMusicRecognition.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AuddMusicRecognition.Web;

/// <summary>
/// Injects the client script into Jellyfin Web's index.html.
/// </summary>
public static partial class IndexHtmlInjector
{
    private const string ScriptRegexPattern = """<script\s+[^>]*data-audd-music-recognition="player-overlay"[^>]*>\s*</script>""";

    /// <summary>
    /// Transforms index.html content when invoked by FileTransformation plugin.
    /// </summary>
    /// <param name="payload">Transformation payload.</param>
    /// <returns>Transformed index.html contents.</returns>
    public static string FileTransformer(PatchRequestPayload payload)
    {
        var html = payload.Contents ?? string.Empty;
        return Inject(html);
    }

    /// <summary>
    /// Directly patches Jellyfin Web's index.html when FileTransformation is unavailable.
    /// </summary>
    public static void Direct()
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return;
        }

        var webPath = plugin.ViewableApplicationPaths.WebPath;
        if (string.IsNullOrWhiteSpace(webPath))
        {
            plugin.Logger.LogWarning("Jellyfin Web path is empty; cannot inject AudD Music Recognition overlay.");
            return;
        }

        var indexFile = Path.Combine(webPath, "index.html");
        if (!File.Exists(indexFile))
        {
            plugin.Logger.LogWarning("Jellyfin Web index.html was not found at {IndexFile}", indexFile);
            return;
        }

        var html = File.ReadAllText(indexFile);
        var injected = Inject(html);

        if (string.Equals(html, injected, StringComparison.Ordinal))
        {
            plugin.Logger.LogInformation("AudD Music Recognition overlay is already injected in {IndexFile}", indexFile);
            return;
        }

        try
        {
            File.WriteAllText(indexFile, injected);
            plugin.Logger.LogInformation("Injected AudD Music Recognition overlay into {IndexFile}", indexFile);
        }
        catch (Exception ex)
        {
            plugin.Logger.LogError(ex, "Failed to write AudD Music Recognition overlay into {IndexFile}", indexFile);
        }
    }

    private static string Inject(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        var cleanedHtml = ScriptTagRegex().Replace(html, string.Empty);
        var scriptElement = GetScriptElement();
        var bodyClosing = cleanedHtml.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        if (bodyClosing >= 0)
        {
            return cleanedHtml.Insert(bodyClosing, scriptElement);
        }

        var htmlClosing = cleanedHtml.LastIndexOf("</html>", StringComparison.OrdinalIgnoreCase);
        return htmlClosing >= 0 ? cleanedHtml.Insert(htmlClosing, scriptElement) : cleanedHtml;
    }

    private static string GetScriptElement()
    {
        var basePath = GetBasePath();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        return $"<script data-audd-music-recognition=\"player-overlay\" version=\"{version}\" src=\"{basePath}/Plugins/AuddMusicRecognition/Web/audd-music-recognition.plugin.js\"></script>";
    }

    private static string GetBasePath()
    {
        try
        {
            NetworkConfiguration? networkConfiguration = Plugin.Instance?.ServerConfigurationManager.GetNetworkConfiguration();
            var baseUrl = networkConfiguration?.GetType().GetProperty("BaseUrl")?.GetValue(networkConfiguration)?.ToString()?.Trim('/');
            return string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : $"/{baseUrl}";
        }
        catch (Exception ex)
        {
            Plugin.Instance?.Logger.LogError(ex, "Unable to read Jellyfin base URL for AudD Music Recognition overlay injection.");
            return string.Empty;
        }
    }

    [GeneratedRegex(ScriptRegexPattern, RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();
}
