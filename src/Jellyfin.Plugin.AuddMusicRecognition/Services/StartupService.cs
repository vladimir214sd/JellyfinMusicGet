using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AuddMusicRecognition.Web;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AuddMusicRecognition.Services;

/// <summary>
/// Runs on Jellyfin startup to inject the player overlay script into Jellyfin Web.
/// </summary>
public sealed class StartupService : IScheduledTask
{
    private readonly ILogger<StartupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "AudD Music Recognition Startup";

    /// <inheritdoc />
    public string Key => "Jellyfin.Plugin.AuddMusicRecognition.Startup";

    /// <inheritdoc />
    public string Description => "Injects the AudD Music Recognition player overlay into Jellyfin Web.";

    /// <inheritdoc />
    public string Category => "Startup Services";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (Plugin.Instance?.Configuration.EnableWebOverlay != true)
        {
            _logger.LogInformation("AudD Music Recognition web overlay injection is disabled.");
            return Task.CompletedTask;
        }

        if (!TryRegisterFileTransformation())
        {
            IndexHtmlInjector.Direct();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.StartupTrigger
        };
    }

    private bool TryRegisterFileTransformation()
    {
        var fileTransformationAssembly = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(assembly => assembly.FullName?.Contains(".FileTransformation") == true);

        if (fileTransformationAssembly is null)
        {
            _logger.LogInformation("FileTransformation plugin not found. Falling back to direct index.html injection.");
            return false;
        }

        var pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
        var registerMethod = pluginInterfaceType?.GetMethod("RegisterTransformation");

        if (registerMethod is null)
        {
            _logger.LogInformation("FileTransformation RegisterTransformation API not found. Falling back to direct index.html injection.");
            return false;
        }

        var parameterType = registerMethod.GetParameters().FirstOrDefault()?.ParameterType;
        var parseMethod = parameterType?.GetMethod("Parse", [typeof(string)]);

        if (parseMethod is null)
        {
            _logger.LogInformation("FileTransformation RegisterTransformation payload parser not found. Falling back to direct index.html injection.");
            return false;
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            id = "ad3000ca-4bcb-4b4d-a67f-b9a80cd81892",
            fileNamePattern = "index.html",
            callbackAssembly = GetType().Assembly.FullName,
            callbackClass = typeof(IndexHtmlInjector).FullName,
            callbackMethod = nameof(IndexHtmlInjector.FileTransformer)
        });

        try
        {
            var payload = parseMethod.Invoke(null, [payloadJson]);
            registerMethod.Invoke(null, [payload]);
            _logger.LogInformation("Registered AudD Music Recognition web overlay with FileTransformation plugin.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FileTransformation registration failed. Falling back to direct index.html injection.");
            return false;
        }
    }
}
