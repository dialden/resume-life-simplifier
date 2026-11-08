using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ApplicationSubmissionVerification(
    VoidValue response, UseCaseContext context, IRLSDriver driver,
    int postingId, string email, byte[] resume) : ResponseVerification<VoidValue>(response, context)
{
    public async Task<ApplicationResponse> FindForPosting(string idAlias)
    {
        var id = ResolvePostingId(idAlias);
        id.ShouldBe(postingId);
        var result = await driver.ListApplications(id);
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);
        var matches = result.Value.Where(application => application.Email == email).ToArray();
        matches.ShouldNotBeEmpty();
        return matches[^1];
    }

    public async Task HasSubmittedResume(ApplicationResponse application)
    {
        var result = await driver.DownloadResume(postingId, application.Id);
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);
        result.Value.ShouldBe(resume);
    }

    public async Task IsNotListedForPosting(string idAlias)
    {
        var id = ResolvePostingId(idAlias);
        id.ShouldNotBe(postingId);
        var result = await driver.ListApplications(id);
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);
        result.Value.Any(application => application.Email == email).ShouldBeFalse();
    }

    public async Task HasApplicationCountForEmail(int count)
    {
        var result = await driver.ListApplications(postingId);
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);
        result.Value.Count(application => application.Email == email).ShouldBe(count);
    }

    private int ResolvePostingId(string idAlias)
    {
        if (!int.TryParse(Context.GetResultValue(idAlias), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var id))
        {
            throw new InvalidOperationException($"Posting ID alias '{idAlias}' did not resolve to an integer ID.");
        }
        return id;
    }
}
