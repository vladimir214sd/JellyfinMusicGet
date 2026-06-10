using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Plugin.AuddMusicRecognition.Middleware;

/// <summary>
/// Injects the player overlay script into Jellyfin Web HTML responses.
/// </summary>
public sealed partial class WebOverlayInjectionMiddleware
{
    private const string ScriptMarker = "data-audd-music-recognition=\"player-overlay\"";
    private const string ScriptTag = "<script data-audd-music-recognition=\"player-overlay\" src=\"/Plugins/AuddMusicRecognition/Web/audd-music-recognition.plugin.js\" defer></script>";

    private readonly RequestDelegate _next;
    private readonly ILogger<WebOverlayInjectionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebOverlayInjectionMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next middleware.</param>
    /// <param name="logger">Logger.</param>
    public WebOverlayInjectionMiddleware(RequestDelegate next, ILogger<WebOverlayInjectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldInspectRequest(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);

            buffer.Position = 0;

            if (!ShouldInjectResponse(context.Response))
            {
                context.Response.Body = originalBody;
                await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
                return;
            }

            using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var html = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
            var injected = InjectScript(html);

            context.Response.Body = originalBody;

            if (ReferenceEquals(injected, html))
            {
                await WriteHtmlAsync(context, html).ConfigureAwait(false);
                return;
            }

            context.Response.Headers.ContentLength = Encoding.UTF8.GetByteCount(injected);
            await WriteHtmlAsync(context, injected).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.Response.Body = originalBody;
            LogInjectionFailed(_logger, ex);
            throw;
        }
    }

    private static bool ShouldInspectRequest(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        if (Plugin.Instance?.Configuration.EnableWebOverlay != true)
        {
            return false;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/Plugins/AuddMusicRecognition", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".map", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var accept = context.Request.Headers.Accept.ToString();
        return string.IsNullOrWhiteSpace(accept)
            || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
            || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldInjectResponse(HttpResponse response)
    {
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            return false;
        }

        if (response.Headers.TryGetValue("Content-Encoding", out StringValues encoding)
            && !StringValues.IsNullOrEmpty(encoding))
        {
            return false;
        }

        return response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string InjectScript(string html)
    {
        if (html.Contains(ScriptMarker, StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }

        var bodyIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyIndex >= 0)
        {
            return html.Insert(bodyIndex, ScriptTag + Environment.NewLine);
        }

        var htmlIndex = html.LastIndexOf("</html>", StringComparison.OrdinalIgnoreCase);
        if (htmlIndex >= 0)
        {
            return html.Insert(htmlIndex, ScriptTag + Environment.NewLine);
        }

        return html;
    }

    private static async Task WriteHtmlAsync(HttpContext context, string html)
    {
        var bytes = Encoding.UTF8.GetBytes(html);
        await context.Response.Body.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to inject AudD Music Recognition web overlay")]
    private static partial void LogInjectionFailed(ILogger logger, Exception exception);
}
