using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeasonalPicks.Web;

/// <summary>
/// Adds the For You script to the web client's index.html through the File Transformation plugin
/// (called by reflection, so it's optional). TV apps bundle their own client and never see this;
/// they get the playlists and mirror folder instead.
/// </summary>
public class WebInjectionService : IHostedService
{
    private static readonly Guid TransformationId = new("0f5c3a8e-1d2b-4c6f-9a7e-3b8d5e2f1a90");

    private readonly ILogger<WebInjectionService> _logger;

    public WebInjectionService(ILogger<WebInjectionService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var assemblies = AssemblyLoadContext.All.SelectMany(c => c.Assemblies).ToList();
            var ft = assemblies.FirstOrDefault(a => a.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) == true);
            var register = ft?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")?.GetMethod("RegisterTransformation");
            if (register is null)
            {
                _logger.LogWarning("SeasonalPicks: File Transformation plugin not found; the web Suggested screen is disabled (playlists still work)");
                return Task.CompletedTask;
            }

            var jobject = assemblies
                .Where(a => a.GetName().Name == "Newtonsoft.Json")
                .Select(a => a.GetType("Newtonsoft.Json.Linq.JObject"))
                .FirstOrDefault(t => t is not null);
            var parse = jobject?.GetMethod("Parse", new[] { typeof(string) });
            if (parse is null)
            {
                _logger.LogWarning("SeasonalPicks: Newtonsoft.Json not loaded; cannot register web injection");
                return Task.CompletedTask;
            }

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = TransformationId.ToString(),
                // Regex: escape and anchor, or it also matches JS chunks like itemDetails-index-html.*.js.
                fileNamePattern = @"(^|/)index\.html$",
                callbackAssembly = typeof(IndexTransformer).Assembly.FullName,
                callbackClass = typeof(IndexTransformer).FullName,
                callbackMethod = nameof(IndexTransformer.Transform)
            });

            register.Invoke(null, new[] { parse.Invoke(null, new object[] { payload }) });
            _logger.LogInformation("SeasonalPicks: registered web client injection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SeasonalPicks: failed to register web client injection");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class TransformPayload
{
    [JsonPropertyName("contents")]
    public string? Contents { get; set; }
}

public static class IndexTransformer
{
    private const string Marker = "SeasonalPicks/ClientScript";

    public static string Transform(TransformPayload payload)
    {
        var html = payload.Contents ?? string.Empty;
        try
        {
            if (!html.Contains("</head>", StringComparison.Ordinal)
                || !html.Contains("</body>", StringComparison.Ordinal)
                || html.Contains(Marker, StringComparison.Ordinal)
                || Plugin.Instance?.Configuration.EnableWebScreen == false)
            {
                return html;
            }

            // Relative: index.html lives at {baseUrl}/web/.
            var tag = "<script defer src=\"../" + Marker + "\"></script>";
            var at = html.LastIndexOf("</body>", StringComparison.Ordinal);
            return html.Insert(at, tag);
        }
        catch
        {
            return html;
        }
    }
}
