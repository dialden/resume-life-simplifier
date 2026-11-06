using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public sealed class ThenPublicListing<TResponse, TVerification>
    where TVerification : ResponseVerification<TResponse>
{
    private readonly ThenStage<TResponse, TVerification> _stage;
    private readonly List<Func<PublicListingVerification, Task>> _checks = [];

    internal ThenPublicListing(ThenStage<TResponse, TVerification> stage) => _stage = stage;

    public ThenPublicListing<TResponse, TVerification> HasTitle(string title)
    {
        _checks.Add(listing =>
        {
            listing.HasTitle(title);
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenPublicListing<TResponse, TVerification> HasDescription(string description)
    {
        _checks.Add(listing =>
        {
            listing.HasDescription(description);
            return Task.CompletedTask;
        });
        return this;
    }

    public ThenPublicListing<TResponse, TVerification> HasStableShareableAddress()
    {
        _checks.Add(async listing =>
        {
            listing.HasShareableAddress();
            var revisited = await _stage.App.RLS.ViewPublicListing()
                .WithId(listing.Id).AtAddress(listing.Address).Execute();
            var current = revisited.ShouldSucceed();
            current.HasShareableAddress();
            current.Matches(listing);
        });
        return this;
    }

    public ThenPublicListing<TResponse, TVerification> IsLinkedFromJobSite(string jobSiteId)
    {
        _checks.Add(async listing =>
        {
            await _stage.App.RLS.ViewPosting().WithId(listing.Id)
                .UntilJobSiteResponded(jobSiteId, TimeSpan.FromSeconds(30), "Succeeded");
            await _stage.App.ExternalSystems.JobSites.HasPublicListingAddress(
                jobSiteId, listing.Id, listing.Address);
        });
        return this;
    }

    public ThenPublicListing<TResponse, TVerification> RemainsAvailableAfterJobSiteRejects(string jobSiteId)
    {
        _checks.Add(async listing =>
        {
            await _stage.App.RLS.ViewPosting().WithId(listing.Id)
                .UntilJobSiteResponded(jobSiteId, TimeSpan.FromSeconds(30), "Failed");
            var revisited = await _stage.App.RLS.ViewPublicListing()
                .WithId(listing.Id).AtAddress(listing.Address).Execute();
            revisited.ShouldSucceed().Matches(listing);
        });
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    private async Task Execute()
    {
        var execution = await _stage.GetExecutionResult();
        var outcome = execution.Result.ShouldSucceed();
        PublicListingVerification listing;
        switch (outcome)
        {
            case PublishPostingVerification published:
                var publicView = await _stage.App.RLS.ViewPublicListing()
                    .WithId(published.Id).AtAddress(published.PublicListingUrl).Execute();
                listing = publicView.ShouldSucceed();
                listing.Address.ShouldBe(published.PublicListingUrl);
                break;
            case PublicListingVerification viewed:
                listing = viewed;
                break;
            default:
                throw new InvalidOperationException("PublicListing() requires publishing or viewing a public listing.");
        }

        foreach (var check in _checks)
        {
            await check(listing);
        }
    }
}
