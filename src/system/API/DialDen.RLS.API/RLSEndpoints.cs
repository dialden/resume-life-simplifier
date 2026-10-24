using DialDen.RLS.Abstractions.Postings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DialDen.RLS.API;

public static class RLSEndpoints
{
    public static IEndpointRouteBuilder MapRLSEndpoints(this IEndpointRouteBuilder app)
    {
        // Advertise the actions hypermedia clients can follow.
        app.MapGet("/", () => TypedResults.Ok(new RLSEntryPoint([
                new Link("create-posting", "/postings", "POST"),
                new Link("view-posting", "/postings/{id}", "GET")
            ])))
            .WithName("EntryPoint")
            .WithSummary("API entry point returning links to the available actions.")
            .Produces<RLSEntryPoint>(StatusCodes.Status200OK);

        app.MapPost("/postings", async (
                CreatePostingRequest request,
                [FromServices] ICreatePosting createPosting,
                CancellationToken cancellationToken) =>
            {
                var posting = await createPosting.Create(request, cancellationToken);
                return TypedResults.Created($"/postings/{posting.Id}", posting);
            })
            .WithName("CreatePosting")
            .WithSummary("Creates a job posting.")
            .Accepts<CreatePostingRequest>("application/json")
            .Produces<CreatePostingResponse>(StatusCodes.Status201Created);

        app.MapGet("/postings/{id:int}", async Task<IResult> (
                int id,
                [FromServices] IViewPosting viewPosting,
                CancellationToken cancellationToken) =>
            {
                var posting = await viewPosting.View(new ViewPostingRequest(id), cancellationToken);
                if (posting is null)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(posting);
            })
            .WithName("ViewPosting")
            .WithSummary("Retrieves a job posting by its identifier.")
            .Produces<ViewPostingResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}