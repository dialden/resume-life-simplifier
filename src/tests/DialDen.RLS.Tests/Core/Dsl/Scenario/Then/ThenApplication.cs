using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public sealed class ThenApplication<TResponse, TVerification>
    where TVerification : ResponseVerification<TResponse>
{
    private readonly ThenStage<TResponse, TVerification> _stage;
    private readonly string _postingIdAlias;
    private readonly List<Func<ApplicationSubmissionVerification, ApplicationResponse, Task>> _checks = [];

    internal ThenApplication(ThenStage<TResponse, TVerification> stage, string postingIdAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postingIdAlias);
        _stage = stage;
        _postingIdAlias = postingIdAlias;
    }

    public ThenApplication<TResponse, TVerification> HasName(string name)
    {
        _checks.Add((_, application) =>
        {
            application.Name.ShouldBe(name);
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenApplication<TResponse, TVerification> HasEmail(string email)
    {
        _checks.Add((_, application) =>
        {
            application.Email.ShouldBe(email);
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenApplication<TResponse, TVerification> HasSource(string source)
    {
        _checks.Add((_, application) =>
        {
            application.Source.ShouldBe(source);
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenApplication<TResponse, TVerification> HasSubmittedResume()
    {
        _checks.Add((submission, application) => submission.HasSubmittedResume(application));
        return this;
    }

    public ThenApplication<TResponse, TVerification> IsAwaitingInformationExtraction()
    {
        _checks.Add((_, application) =>
        {
            application.ExtractionStatus.ShouldBe("Pending");
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenApplication<TResponse, TVerification> EventuallyHasFailedExtraction()
    {
        _checks.Add((submission, application) => submission.EventuallyHasFailedExtraction(application));
        return this;
    }

    public ThenApplication<TResponse, TVerification> EventuallyHasExtractedContactInformation(
        string? name = null, string? email = null, string? phoneNumber = null,
        string? postalAddress = null, string? linkedInUrl = null)
    {
        var expected = new ExtractedContactInformation(name, email, phoneNumber, postalAddress, linkedInUrl);
        _checks.Add((submission, application) =>
            submission.EventuallyHasExtractedContactInformation(application, expected));
        return this;
    }

    public ThenApplication<TResponse, TVerification> WasReferredBy(string jobSiteId)
    {
        _checks.Add((_, application) =>
        {
            application.ReferralJobSiteId.ShouldBe(jobSiteId);
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenApplication<TResponse, TVerification> WasNotReferredByAJobSite()
    {
        _checks.Add((_, application) =>
        {
            application.ReferralJobSiteId.ShouldBeNull();
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenApplication<TResponse, TVerification> IsNotListedForPosting(string idAlias)
    {
        _checks.Add((submission, _) => submission.IsNotListedForPosting(idAlias));
        return this;
    }

    public ThenApplication<TResponse, TVerification> HasApplicationCountForEmail(int count)
    {
        _checks.Add((submission, _) => submission.HasApplicationCountForEmail(count));
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    private async Task Execute()
    {
        var execution = await _stage.GetExecutionResult();
        var outcome = execution.Result.ShouldSucceed();
        if (outcome is not ApplicationSubmissionVerification submission)
        {
            throw new InvalidOperationException("ApplicationForPosting() requires submitting an application.");
        }

        var application = await submission.FindForPosting(_postingIdAlias);
        foreach (var check in _checks)
        {
            await check(submission, application);
        }
    }
}
