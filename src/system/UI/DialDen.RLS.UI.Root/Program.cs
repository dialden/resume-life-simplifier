using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DialDen.RLS.UI;
using DialDen.RLS.UI.DependencyInjection;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("'ApiBaseUrl' is not configured in wwwroot/appsettings.json.");
builder.Services.AddRLSUI(new Uri(apiBaseUrl));

var host = builder.Build();
await host.Services.UseRLSLanguageAsync();
await host.RunAsync();
