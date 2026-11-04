using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.API.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace DialDen.RLS.API;

public static class RLSEndpoints
{
    public static IEndpointRouteBuilder MapRLSEndpoints(this IEndpointRouteBuilder app)
    {
        // Advertise the actions hypermedia clients can follow.
        app.MapGet("/", () => TypedResults.Ok(new RLSEntryPoint([
                new Link("create-posting", "/postings", "POST"),
                new Link("list-postings", "/postings", "GET"),
                new Link("list-job-sites", "/job-sites", "GET"),
                new Link("publish-posting", "/postings/{id}", "PATCH"),
                new Link("view-posting", "/postings/{id}", "GET")
            ])))
            .WithName("EntryPoint")
            .WithSummary("API entry point returning links to the available actions.")
            .Produces<RLSEntryPoint>(StatusCodes.Status200OK);

        app.MapGet("/job-sites", async ([FromServices] IJobSites jobSites, CancellationToken cancellationToken) =>
                TypedResults.Ok(await jobSites.List(cancellationToken)))
            .WithName("ListJobSites")
            .WithSummary("Lists available external job-site destinations.")
            .Produces<IReadOnlyList<JobSite>>(StatusCodes.Status200OK);

        app.MapPost("/postings", async Task<Results<Created<CreatePostingResponse>, ValidationProblem>> (
                CreatePostingRequest request,
                [FromServices] ICreatePosting createPosting,
                [FromServices] IStringLocalizer<ApiMessages> messages,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var posting = await createPosting.Create(request, cancellationToken);
                    return TypedResults.Created($"/postings/{posting.Id}", posting);
                }
                catch (InvalidPostingDescriptionException)
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["description"] = [messages["PostingDescriptionRequired"]]
                    });
                }
            })
            .WithName("CreatePosting")
            .WithSummary("Creates a job posting.")
            .Accepts<CreatePostingRequest>("application/json")
            .Produces<CreatePostingResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        app.MapGet("/postings", async Task<Results<Ok<ListPostingsResponse>, ValidationProblem>> (
                [FromServices] IListPostings listPostings,
                [FromServices] IStringLocalizer<ApiMessages> messages,
                CancellationToken cancellationToken,
                int pageNumber = 1) =>
            {
                if (pageNumber < 1)
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["pageNumber"] = [messages["PageNumberMustBePositive"]]
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

        app.MapPatch("/postings/{id:int}", async Task<IResult> (
                int id,
                UpdatePostingRequest request,
                [FromServices] IPublishPosting publishPosting,
                [FromServices] IStringLocalizer<ApiMessages> messages,
                CancellationToken cancellationToken) =>
            {
                if (request.Status is not PostingStatus.Published)
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["status"] = [messages["OnlyPublishingSupported"]]
                    });
                }

                try
                {
                    var posting = await publishPosting.Publish(
                        new PublishPostingRequest(id) { JobSiteIds = request.JobSiteIds }, cancellationToken);
                    return posting is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(posting);
                }
                catch (PostingAlreadyPublishedException)
                {
                    return TypedResults.Problem(
                        statusCode: StatusCodes.Status409Conflict, detail: messages["PostingAlreadyPublished"]);
                }
                catch (InvalidJobSitesException)
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["jobSiteIds"] = [messages["JobSitesInvalid"]]
                    });
                }
            })
            .WithName("UpdatePosting")
            .WithSummary("Updates a job posting's status.")
            .Accepts<UpdatePostingRequest>("application/json")
            .Produces<PublishPostingResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return app;
    }
}