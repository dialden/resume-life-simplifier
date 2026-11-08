using DialDen.RLS.Abstractions.Postings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DialDen.RLS.API;

public static class EntryPointEndpoints
{
    public static IEndpointRouteBuilder MapEntryPoint(this IEndpointRouteBuilder app)
    {
        // Advertise the actions hypermedia clients can follow.
        app.MapGet("/", () => TypedResults.Ok(new RLSEntryPoint([
                new Link("create-posting", "/postings", "POST"),
                new Link("list-postings", "/postings", "GET"),
                new Link("list-job-sites", "/job-sites", "GET"),
                new Link("publish-posting", "/postings/{id}", "PATCH"),
                new Link("view-posting", "/postings/{id}", "GET"),
                new Link("view-public-listing", "/public-postings/{id}", "GET"),
                new Link("submit-application", "/public-postings/{id}/applications", "POST"),
                new Link("list-applications", "/postings/{id}/applications", "GET"),
                new Link("download-resume", "/postings/{id}/applications/{applicationId}/resume", "GET"),
                new Link("notifications", "/notifications/stream", "GET")
            ])))
            .WithName("EntryPoint")
            .WithSummary("API entry point returning links to the available actions.")
            .Produces<RLSEntryPoint>(StatusCodes.Status200OK);

        return app;
    }
}
