using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace DialDen.RLS.API;

public static class ApplicationEndpoints
{
    private const int MaxResumeBytes = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/public-postings/{id:int}/applications", async Task<IResult> (
            int id, HttpRequest httpRequest, IApplications applications,
            IStringLocalizer<Messages> Localizer, CancellationToken cancellationToken) =>
        {
            if (!httpRequest.HasFormContentType)
                return Invalid("resume", Localizer);

            IFormCollection form;
            try
            {
                form = await httpRequest.ReadFormAsync(cancellationToken);
            }
            catch (InvalidDataException)
            {
                return Invalid("resume", Localizer);
            }
            var resume = form.Files.GetFile("resume");
            if (resume is null || resume.Length == 0 || resume.Length > MaxResumeBytes)
                return Invalid("resume", Localizer);
            await using var input = resume.OpenReadStream();
            using var output = new MemoryStream();
            await input.CopyToAsync(output, cancellationToken);
            try
            {
                var applicationId = await applications.Submit(new SubmitApplicationRequest(
                    id, form["name"].ToString(), form["email"].ToString(), resume.FileName,
                    output.ToArray(), form["referralJobSiteId"].FirstOrDefault()), cancellationToken);
                return applicationId is null
                    ? TypedResults.NotFound()
                    : TypedResults.Created($"/postings/{id}/applications/{applicationId}", new { id = applicationId });
            }
            catch (InvalidApplicationException exception)
            {
                return Invalid(exception.Field, Localizer);
            }
        })
            .WithName("SubmitApplication")
            .WithSummary("Submits an application to a published posting.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .WithMetadata(new RequestSizeLimitAttribute(MaxResumeBytes + 64 * 1024));

        app.MapGet("/postings/{id:int}/applications", async Task<IResult> (
            int id, IApplications applications, CancellationToken cancellationToken) =>
        {
            var result = await applications.List(id, cancellationToken);
            return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
        })
            .WithName("ListApplications")
            .WithSummary("Lists applications for a posting.")
            .Produces<IReadOnlyList<ApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/postings/{id:int}/applications/{applicationId:int}/resume", async Task<IResult> (
            int id, int applicationId, IApplications applications, CancellationToken cancellationToken) =>
        {
            var resume = await applications.Resume(id, applicationId, cancellationToken);
            return resume is null ? TypedResults.NotFound()
                : TypedResults.File(resume.Content, "application/octet-stream", resume.FileName);
        })
            .WithName("DownloadResume")
            .WithSummary("Downloads a submitted résumé.")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/postings/{id:int}/applications/{applicationId:int}", async Task<IResult> (
            int id, int applicationId, IApplications applications, CancellationToken cancellationToken) =>
        {
            var application = await applications.Find(id, applicationId, cancellationToken);
            return application is null ? TypedResults.NotFound() : TypedResults.Ok(application);
        })
            .WithName("ViewApplication")
            .WithSummary("Retrieves an application for review.")
            .Produces<ApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);
        return app;
    }

    private static IResult Invalid(string field, IStringLocalizer<Messages> Localizer) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [Localizer[$"Api_ApplicationInvalid_{field}"]]
        });
}
