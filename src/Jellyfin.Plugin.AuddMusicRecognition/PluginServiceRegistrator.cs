using System.Net.Http;
using Jellyfin.Plugin.AuddMusicRecognition.Middleware;
using Jellyfin.Plugin.AuddMusicRecognition.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AuddMusicRecognition;

/// <summary>
/// Registers plugin services with Jellyfin's dependency injection container.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<HttpClient>();
        serviceCollection.AddSingleton<IAuddClient, AuddClient>();
        serviceCollection.AddSingleton<IAudioClipExtractor, FfmpegAudioClipExtractor>();
        serviceCollection.AddSingleton<IRecognitionService, RecognitionService>();
        serviceCollection.AddSingleton<IStartupFilter, WebOverlayStartupFilter>();
    }
}
