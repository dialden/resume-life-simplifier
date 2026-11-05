using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DialDen.RLS.UI;
using DialDen.RLS.UI.DependencyInjection;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();
builder.Services.AddRLSUI(builder.Configuration);

var host = builder.Build();
await host.Services.UseRLSLanguageAsync();
await host.RunAsync();
