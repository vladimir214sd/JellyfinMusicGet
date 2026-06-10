using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.AuddMusicRecognition.Middleware;

/// <summary>
/// Adds the Jellyfin Web overlay injector to the ASP.NET Core pipeline.
/// </summary>
public sealed class WebOverlayStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<WebOverlayInjectionMiddleware>();
            next(builder);
        };
    }
}
