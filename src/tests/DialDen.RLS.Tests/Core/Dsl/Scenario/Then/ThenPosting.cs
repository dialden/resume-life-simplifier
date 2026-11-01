using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public class ThenPosting<TSuccessResponse, TSuccessVerification>
    where TSuccessVerification : ResponseVerification<TSuccessResponse>
{
    private readonly ThenStage<TSuccessResponse, TSuccessVerification> _stage;
    private readonly List<Action<object>> _verifications = [];
    private readonly List<Func<object, Task>> _observations = [];

    internal ThenPosting(ThenStage<TSuccessResponse, TSuccessVerification> stage)
    {
        _stage = stage;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasTitle(string expectedTitle)
    {
        _verifications.Add(v =>
        {
            if (v is not CreatePostingVerification creation)
            {
                throw new InvalidOperationException("Posting titles can only be verified when creating a posting.");
            }

            creation.HasTitle(expectedTitle);
        });
        return this;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasStatus(string expectedStatus)
    {
        _verifications.Add(v =>
        {
            if (v is not IPostingStatusVerification posting)
            {
                throw new InvalidOperationException("Posting status can only be verified for a posting operation.");
            }

            posting.HasStatus(expectedStatus);
        });
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    public ThenPosting<TSuccessResponse, TSuccessVerification> EventuallyHasJobSitePublication(
        string jobSiteId, string expectedStatus)
    {
        if (expectedStatus is not ("Succeeded" or "Failed"))
        {
            throw new ArgumentException("Expected a final publication status: Succeeded or Failed.", nameof(expectedStatus));
        }
        _observations.Add(async verification =>
        {
            if (verification is not PublishPostingVerification publication)
            {
                throw new InvalidOperationException("Eventual publication outcomes can only be verified after publishing.");
            }
            await _stage.App.RLS.ViewPosting().WithId(publication.Id).UntilJobSiteResponded(
                jobSiteId, TimeSpan.FromSeconds(30), expectedStatus);
        });
        return this;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasJobSitePublication(string jobSiteId, string expectedStatus)
    {
        _verifications.Add(v =>
        {
            if (v is not IJobSitePublicationVerification posting)
            {
                throw new InvalidOperationException("Job-site publications can only be verified for publishing or viewing a posting.");
            }
            posting.HasJobSitePublication(jobSiteId, expectedStatus);
        });
        return this;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasNoJobSitePublications()
    {
        _verifications.Add(v =>
        {
            if (v is not IJobSitePublicationVerification posting)
            {
                throw new InvalidOperationException("Job-site publications can only be verified for publishing or viewing a posting.");
            }
            posting.HasNoJobSitePublications();
        });
        return this;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasJobSitePublicationCount(int count)
    {
        _verifications.Add(v =>
        {
            if (v is not IJobSitePublicationVerification posting)
            {
                throw new InvalidOperationException("Job-site publications can only be verified for publishing or viewing a posting.");
            }
            posting.HasJobSitePublicationCount(count);
        });
        return this;
    }

    private async Task Execute()
    {
        var result = await _stage.GetExecutionResult();
        var verification = result.Result.ShouldSucceed();

        foreach (var check in _verifications)
        {
            check(verification);
        }
        foreach (var observation in _observations)
        {
            await observation(verification);
        }
    }
}
