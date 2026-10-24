using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeasonalPicks.Web;

/// <summary>
/// Adds the For You script to the web client's index.html through the File Transformation plugin
/// (called by reflection, so it's optional). TV apps bundle their own client and never see this;
/// they get the playlists and mirror folder instead.
/// </summary>
public sealed class WebInjectionService : IHostedService, IDisposable
{
    private static readonly Guid TransformationId = new("0f5c3a8e-1d2b-4c6f-9a7e-3b8d5e2f1a90");

    // How long to keep retrying the registration at startup, and how often to check afterwards that the page has the script.
    private const int RegisterAttempts = 60;
    private static readonly TimeSpan RegisterRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    private readonly ILogger<WebInjectionService> _logger;
    private readonly IServerConfigurationManager _config;
    private readonly IServerApplicationHost _host;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CancellationTokenSource _stopping = new();

    // File Transformation copies already registered with.
    private readonly HashSet<Assembly> _registered = new();

    public WebInjectionService(
        ILogger<WebInjectionService> logger,
        IServerConfigurationManager config,
        IServerApplicationHost host,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config;
        _host = host;
        _httpClientFactory = httpClientFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Not awaited: startup mustn't wait for the retries or the checks.
        _ = Task.Run(() => RunAsync(_stopping.Token));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose() => _stopping.Dispose();

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            if (!await RegisterWithRetriesAsync(token).ConfigureAwait(false))
            {
                return;
            }

            // File Transformation can be reloaded or updated without a restart, which drops the registration.
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, token).ConfigureAwait(false);
                await CheckAndRepairAsync(token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Server shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SeasonalPicks: the web client injection watcher stopped unexpectedly");
        }
    }

    // File Transformation 3.x resolves its services from the provider it was created with. Jellyfin can load it more
    // than once during startup, and a copy whose provider is already disposed throws ObjectDisposedException inside
    // RegisterTransformation. So keep trying for the whole window and register with every copy that accepts, since the
    // copy that serves pages may only appear after an earlier one accepted. false: File Transformation can't be used.
    private async Task<bool> RegisterWithRetriesAsync(CancellationToken token)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= RegisterAttempts; attempt++)
        {
            var result = TryRegister(out var error);
            if (result is null)
            {
                return false;
            }

            lastError = error ?? lastError;
            if (attempt == 1 && _registered.Count == 0)
            {
                _logger.LogInformation("SeasonalPicks: File Transformation isn't ready yet ({Reason}); retrying", error?.GetBaseException().Message);
            }

            await Task.Delay(RegisterRetryDelay, token).ConfigureAwait(false);
        }

        if (_registered.Count == 0)
        {
            _logger.LogError(lastError, "SeasonalPicks: File Transformation didn't accept the web client injection after {Attempts} attempts; the web Suggested screen is off until Jellyfin restarts (playlists still work)", RegisterAttempts);
        }

        // Keep watching even after a failure: a later repair may still work.
        return true;
    }

    private async Task CheckAndRepairAsync(CancellationToken token)
    {
        // Turned off in the settings: the page is meant to be without the script.
        if (Plugin.Instance?.Configuration.EnableWebScreen == false || await PageHasScriptAsync(token).ConfigureAwait(false) != false)
        {
            return;
        }

        _logger.LogWarning("SeasonalPicks: the web client's index.html no longer has the For You script; registering again");
        _registered.Clear();
        TryRegister(out var error);
        if (await PageHasScriptAsync(token).ConfigureAwait(false) == false)
        {
            _logger.LogError(error, "SeasonalPicks: registering again didn't bring the For You script back; restart Jellyfin");
        }
    }

    // Fetches index.html from this server the way browsers get it (through File Transformation). null when the check
    // itself couldn't run, which isn't treated as a missing script.
    private async Task<bool?> PageHasScriptAsync(CancellationToken token)
    {
        try
        {
            var baseUrl = (_config.GetConfiguration<NetworkConfiguration>("network")?.BaseUrl ?? string.Empty).TrimEnd('/');
            var http = _httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            using var response = await http.GetAsync(new Uri($"http://localhost:{_host.HttpPort}{baseUrl}/web/index.html"), token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            return html.Contains(IndexTransformer.Marker, StringComparison.Ordinal);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SeasonalPicks: could not fetch index.html to check the web client injection");
            return null;
        }
    }

    // Registers with every loaded File Transformation copy not registered yet. false: none has accepted yet (error says
    // why; a later try may work). null: File Transformation isn't usable at all. true otherwise.
    private bool? TryRegister(out Exception? error)
    {
        error = null;
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            id = TransformationId.ToString(),
            // Must be exactly "index.html". File Transformation groups transformations by this string and runs one group
            // per file (an exact name wins), and other plugins register "index.html", so any other pattern never runs for
            // the real page. The group also runs on chunks like itemDetails-index-html.*.js; IndexTransformer leaves
            // anything that isn't a whole HTML page alone.
            fileNamePattern = "index.html",
            callbackAssembly = typeof(IndexTransformer).Assembly.FullName,
            callbackClass = typeof(IndexTransformer).FullName,
            callbackMethod = nameof(IndexTransformer.Transform)
        });

        // Every loaded copy, newest first; the first one found may be one whose services were disposed.
        var candidates = AssemblyLoadContext.All.SelectMany(c => c.Assemblies)
            .Concat(AppDomain.CurrentDomain.GetAssemblies())
            .Where(a => a.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) == true)
            .Distinct()
            .Reverse()
            .ToList();
        if (candidates.Count == 0)
        {
            _logger.LogWarning("SeasonalPicks: File Transformation plugin not found; the web Suggested screen is disabled (playlists still work)");
            return null;
        }

        var anyUsable = false;
        foreach (var assembly in candidates)
        {
            var register = assembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")?.GetMethod("RegisterTransformation");
            var parameters = register?.GetParameters();
            if (register is null || parameters is null || parameters.Length != 1)
            {
                continue;
            }

            anyUsable = true;
            if (_registered.Contains(assembly))
            {
                continue;
            }

            try
            {
                // File Transformation takes a Newtonsoft JObject: build it with the JObject type this copy was compiled
                // against (its own parameter type), so a second Newtonsoft copy can't cause a type mismatch.
                var parse = parameters[0].ParameterType.GetMethod("Parse", new[] { typeof(string) });
                if (parse is null)
                {
                    continue;
                }

                register.Invoke(null, new[] { parse.Invoke(null, new object[] { payload }) });
                _registered.Add(assembly);
                _logger.LogInformation("SeasonalPicks: registered web client injection (File Transformation copy {Index} of {Count})", candidates.IndexOf(assembly) + 1, candidates.Count);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        if (!anyUsable)
        {
            _logger.LogWarning("SeasonalPicks: File Transformation has no usable RegisterTransformation method (incompatible version); the web Suggested screen is disabled");
            return null;
        }

        return _registered.Count > 0;
    }
}

public class TransformPayload
{
    [JsonPropertyName("contents")]
    public string? Contents { get; set; }
}

public static class IndexTransformer
{
    internal const string Marker = "SeasonalPicks/ClientScript";

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
