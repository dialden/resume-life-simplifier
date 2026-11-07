using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using DialDen.RLS.UI;
using DialDen.RLS.UI.DependencyInjection;
using DialDen.RLS.UI.Language;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();
builder.Services.AddRLSUI();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("'ApiBaseUrl' is not configured in wwwroot/appsettings.json.");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

var host = builder.Build();

var js = host.Services.GetRequiredService<IJSRuntime>();
var language = await js.InvokeAsync<string?>("localStorage.getItem", LanguageState.StorageKey) ?? "fr";
LanguageState.Apply(new CultureInfo(language));
await js.InvokeVoidAsync("rlsSetDocumentLanguage", language);

await host.RunAsync();
