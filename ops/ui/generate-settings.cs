#!/usr/bin/env -S dotnet run

using System.Text.Json;

var apiPublicUrl = Environment.GetEnvironmentVariable("API_PUBLIC_URL");
if (string.IsNullOrWhiteSpace(apiPublicUrl))
{
    throw new InvalidOperationException("API_PUBLIC_URL must contain the browser-accessible API URL.");
}

var settingsDirectory = Path.GetFullPath("src/system/UI/DialDen.RLS.UI.Root/wwwroot");
if (!Directory.Exists(settingsDirectory))
{
    throw new DirectoryNotFoundException("Could not find the UI wwwroot directory. Run this script from the repository root.");
}

await using var stream = File.Create(Path.Combine(settingsDirectory, "appsettings.json"));
await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
writer.WriteStartObject();
writer.WriteString("ApiBaseUrl", apiPublicUrl);
writer.WriteEndObject();
await writer.FlushAsync();
