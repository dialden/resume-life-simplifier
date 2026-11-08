using DialDen.RLS.Abstractions.Postings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DialDen.RLS.API;

public static class JobSiteEndpoints
{
    public static IEndpointRouteBuilder MapJobSiteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/job-sites", async ([FromServices] IJobSites jobSites, CancellationToken cancellationToken) =>
                TypedResults.Ok(await jobSites.List(cancellationToken)))
            .WithName("ListJobSites")
            .WithSummary("Lists available external job-site destinations.")
            .Produces<IReadOnlyList<JobSite>>(StatusCodes.Status200OK);

        return app;
    }
}
