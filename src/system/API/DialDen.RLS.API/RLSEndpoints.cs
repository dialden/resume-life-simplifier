using DialDen.RLS.API.Notifications;
using Microsoft.AspNetCore.Routing;

namespace DialDen.RLS.API;

public static class RLSEndpoints
{
    public static IEndpointRouteBuilder MapRLSEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapEntryPoint();
        app.MapJobSiteEndpoints();
        app.MapPostingEndpoints();
        app.MapPublicListingEndpoints();
        app.MapNotifications();
        app.MapApplicationEndpoints();

        return app;
    }
}
