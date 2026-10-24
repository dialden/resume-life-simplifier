using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace DialDen.RLS.Tests.Extensions;

public static class IServerExtensions
{
    public static string GetWebServerUrl(this IServer server)
    {
        if (server == null)
        {
            throw new InvalidOperationException("Web server is not configured.");
        }

        var addresses = server.Features.Get<IServerAddressesFeature>();
        if (addresses == null || addresses.Addresses.Count == 0)
        {
            throw new InvalidOperationException("Web server addresses are not configured.");
        }
        // Return the first address as the web server URL
        return addresses.Addresses.FirstOrDefault() ?? throw new InvalidOperationException("No web server address found.");

    }

}