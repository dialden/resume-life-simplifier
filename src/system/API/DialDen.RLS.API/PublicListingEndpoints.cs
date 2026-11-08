using DialDen.RLS.Abstractions.Postings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DialDen.RLS.API;

public static class PublicListingEndpoints
{
    public static IEndpointRouteBuilder MapPublicListingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/public-postings/{id:int}", async Task<IResult> (
                int id,
                [FromServices] IViewPublicListing viewPublicListing,
                CancellationToken cancellationToken) =>
            {
                var listing = await viewPublicListing.View(id, cancellationToken);
                return listing is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(listing);
            })
            .WithName("ViewPublicListing")
            .WithSummary("Retrieves the current details of a published job posting.")
            .WithDescription("Draft and unknown postings are not publicly available.")
            .Produces<PublicListingResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
