using DialDen.RLS.Abstractions.Postings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
                new Link("list-postings", "/postings", "GET"),
                new Link("publish-posting", "/postings/{id}/publish", "POST"),
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

        app.MapGet("/postings", async Task<Results<Ok<ListPostingsResponse>, ValidationProblem>> (
                [FromServices] IListPostings listPostings,
                CancellationToken cancellationToken,
                int pageNumber = 1) =>
            {
                if (pageNumber < 1)
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["pageNumber"] = ["Page number must be greater than zero."]
                    });
                }

                return TypedResults.Ok(await listPostings.List(new ListPostingsRequest(pageNumber), cancellationToken));
            })
            .WithName("ListPostings")
            .WithSummary("Lists a page of ten job postings in identifier order.")
            .WithDescription("Use the one-based pageNumber query parameter (default 1). Returns items, totalCount, pageNumber, and pageSize. Pages beyond the library return an empty items array with the total count.")
            .Produces<ListPostingsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

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

        app.MapPost("/postings/{id:int}/publish", async Task<IResult> (
                int id,
                [FromServices] IPublishPosting publishPosting,
                CancellationToken cancellationToken) =>
            {
                var posting = await publishPosting.Publish(new PublishPostingRequest(id), cancellationToken);
                return posting is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(posting);
            })
            .WithName("PublishPosting")
            .WithSummary("Publishes a job posting.")
            .Produces<PublishPostingResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}