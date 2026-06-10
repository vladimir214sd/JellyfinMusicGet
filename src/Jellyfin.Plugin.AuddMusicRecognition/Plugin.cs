using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.AuddMusicRecognition.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AuddMusicRecognition;

/// <summary>
/// Main plugin entry point.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<Plugin> logger,
        IServerConfigurationManager serverConfigurationManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        ViewableApplicationPaths = applicationPaths;
        Logger = logger;
        ServerConfigurationManager = serverConfigurationManager;
    }

    /// <inheritdoc />
    public override string Name => "AudD Music Recognition";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("ad3000ca-4bcb-4b4d-a67f-b9a80cd81892");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Gets application paths exposed by Jellyfin.
    /// </summary>
    internal IApplicationPaths ViewableApplicationPaths { get; }

    /// <summary>
    /// Gets the plugin logger.
    /// </summary>
    internal ILogger<Plugin> Logger { get; }

    /// <summary>
    /// Gets Jellyfin server configuration manager.
    /// </summary>
    internal IServerConfigurationManager ServerConfigurationManager { get; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        ];
    }
}
