#!/usr/bin/env -S dotnet run

using System.Text.Json;
using System.Text.Json.Nodes;

var environment = RequireEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var apiHttpUrl = RequireOrigin("API_HTTP_URL", Uri.UriSchemeHttp);
var apiHttpsUrl = RequireOrigin("API_HTTPS_URL", Uri.UriSchemeHttps);
var uiHttpUrl = RequireOrigin("UI_HTTP_URL", Uri.UriSchemeHttp);
var uiHttpsUrl = RequireOrigin("UI_HTTPS_URL", Uri.UriSchemeHttps);

var apiDirectory = FindProjectDirectory(RequireEnvironmentVariable("API_PROJECT_FILE"));
var uiDirectory = FindProjectDirectory(RequireEnvironmentVariable("UI_PROJECT_FILE"));
foreach (var directory in new[] { apiDirectory, uiDirectory })
{
    if (!Directory.Exists(directory))
    {
        throw new DirectoryNotFoundException(
            $"Could not find {directory}. Run this script from the repository root.");
    }
}

await WriteSettings(apiDirectory, apiHttpUrl, apiHttpsUrl, isUI: false);
await WriteSettings(uiDirectory, uiHttpUrl, uiHttpsUrl, isUI: true);
Console.WriteLine("Generated API and UI launch profiles from the container environment.");

async Task WriteSettings(string directory, string httpUrl, string httpsUrl, bool isUI)
{
    var settings = new JsonObject
    {
        ["$schema"] = "https://json.schemastore.org/launchsettings.json",
        ["profiles"] = new JsonObject
        {
            ["http"] = CreateProfile(httpUrl, isUI),
            ["https"] = CreateProfile($"{httpsUrl};{httpUrl}", isUI)
        }
    };
    var propertiesDirectory = Path.Combine(directory, "Properties");
    Directory.CreateDirectory(propertiesDirectory);
    await File.WriteAllTextAsync(
        Path.Combine(propertiesDirectory, "launchSettings.json"),
        settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
}

JsonObject CreateProfile(string urls, bool isUI)
{
    var profile = new JsonObject
    {
        ["commandName"] = "Project",
        ["dotnetRunMessages"] = true,
        ["launchBrowser"] = true,
        ["applicationUrl"] = urls,
        ["environmentVariables"] = new JsonObject
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment
        }
    };
    if (isUI)
    {
        profile["inspectUri"] =
            "{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}";
    }
    else
    {
        profile["launchUrl"] = "swagger";
    }

    return profile;
}

static string RequireOrigin(string name, string scheme)
{
    var value = RequireEnvironmentVariable(name);
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
        || uri.Scheme != scheme
        || string.IsNullOrEmpty(uri.Host)
        || uri.AbsolutePath != "/"
        || !string.IsNullOrEmpty(uri.Query)
        || !string.IsNullOrEmpty(uri.Fragment)
        || !string.IsNullOrEmpty(uri.UserInfo)
        || value.EndsWith('/'))
    {
        throw new InvalidOperationException(
            $"{name} must be an {scheme} origin without a path or trailing slash.");
    }

    return value;
}

static string RequireEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Set the {name} environment variable.");
    }

    return value;
}

static string FindProjectDirectory(string projectFileName)
{
    if (Path.GetFileName(projectFileName) != projectFileName
        || !projectFileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"{projectFileName} must be a project filename ending in .csproj.");
    }

    var sourceDirectory = Path.GetFullPath("src");
    if (!Directory.Exists(sourceDirectory))
    {
        throw new DirectoryNotFoundException(
            $"Could not find the src directory at {sourceDirectory}. Run this script from the repository root.");
    }

    var matches = Directory.GetFiles(sourceDirectory, projectFileName, SearchOption.AllDirectories);
    if (matches.Length != 1)
    {
        var message = matches.Length == 0
            ? $"Could not find {projectFileName} under {sourceDirectory}."
            : $"Found multiple {projectFileName} files under {sourceDirectory}.";
        throw new InvalidOperationException(message);
    }

    return Path.GetDirectoryName(matches[0])
        ?? throw new InvalidOperationException($"Could not determine the directory containing {matches[0]}.");
}
