using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DialDen.RLS.Tests.Extensions;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>
/// Serves the built (not published) standalone Blazor WebAssembly UI from a minimal Kestrel host,
/// using the static web assets manifest the build generates, like the WebAssembly DevServer does.
/// </summary>
public sealed class InProcessUIHttpHost : IHttpHost
{
    private const string UIApplicationName = "DialDen.RLS.UI.Root";

    private readonly string _uiBuildPath;
    private readonly Func<string> _apiUrl;
    private readonly object _startLock = new();
    private WebApplication? _app;
    private string? _url;

    public string Url => _url ?? throw new InvalidOperationException("The host has not been started.");

    /// <summary>Serves the UI root project build output, whose path is resolved at build time from the project reference.</summary>
    /// <param name="apiUrl">Resolved on each request for the UI configuration, so the API host can start independently.</param>
    public InProcessUIHttpHost(Func<string> apiUrl)
    {
        _apiUrl = apiUrl;
        _uiBuildPath = typeof(InProcessUIHttpHost).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "UIBuildPath")?.Value
            ?? throw new InvalidOperationException("The 'UIBuildPath' assembly metadata is missing. Check the UI project reference.");
    }

    /// <summary>Starts the host once. Subsequent calls are no-ops.</summary>
    public void Start()
    {
        lock (_startLock)
        {
            if (_app != null) return;
            var manifestPath = Path.Combine(_uiBuildPath, $"{UIApplicationName}.staticwebassets.runtime.json");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException(
                    $"UI static web assets manifest not found. Check that the UI project built.", manifestPath);
            }

            // The app assembly is not loadable here (static assets only), so skip hosting startup assembly discovery
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = [$"--{WebHostDefaults.PreventHostingStartupKey}=true"],
                ApplicationName = UIApplicationName,
                ContentRootPath = _uiBuildPath,
                EnvironmentName = Environments.Development
            });
            builder.Configuration["staticWebAssets"] = manifestPath;
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.WebHost.UseStaticWebAssets();

            var app = builder.Build();
            // Overrides the UI's wwwroot/appsettings.json so it targets the API host under test
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/appsettings.json")
                {
                    await context.Response.WriteAsJsonAsync(new { ApiBaseUrl = _apiUrl() });
                    return;
                }
                await next();
            });
            // Endpoints are needed (not just static files) so fingerprinted assets resolve from their plain route
            app.MapStaticAssets(Path.Combine(_uiBuildPath, $"{UIApplicationName}.staticwebassets.endpoints.json"));
            app.MapFallbackToFile("index.html");
            app.Start();
            _url = app.Services.GetRequiredService<IServer>().GetWebServerUrl();
            _app = app;
        }
    }

    public void Dispose()
    {
        _app?.StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
