using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewPosting : RLSUseCase<ViewPostingResponse, ViewPostingVerification>
{
    private int _id;
    private string? _idAlias;

    public ViewPosting(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public ViewPosting WithId(int id)
    {
        _id = id;
        _idAlias = null;
        return this;
    }

    public ViewPosting WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    /// <summary>Views the posting until its publication on the job site is no longer pending.</summary>
    public async Task UntilJobSiteResponded(string jobSiteId, TimeSpan timeout)
    {
        var id = ResolveId();
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            var result = await Driver.ViewPosting(id);
            if (result.IsFailure)
            {
                throw new InvalidOperationException($"Could not view posting {id}: {result.Error}");
            }
            var publication = result.Value.JobSitePublications.SingleOrDefault(p => p.JobSiteId == jobSiteId)
                ?? throw new InvalidOperationException($"Posting {id} was not published to job site '{jobSiteId}'.");
            if (publication.Status != "Pending")
            {
                return;
            }
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException($"Job site '{jobSiteId}' did not respond for posting {id} within {timeout}.");
            }
            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
    }

    public override async Task<RLSUseCaseResult<ViewPostingResponse, ViewPostingVerification>> Execute()
    {
        var result = await Driver.ViewPosting(ResolveId());
        return new RLSUseCaseResult<ViewPostingResponse, ViewPostingVerification>(
            result,
            Context,
            (response, ctx) => new ViewPostingVerification(response, ctx));
    }

    private int ResolveId()
    {
        var id = _id;
        if (_idAlias != null &&
            !int.TryParse(Context.GetResultValue(_idAlias), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
        {
            throw new InvalidOperationException($"Posting ID alias '{_idAlias}' did not resolve to an integer ID.");
        }
        return id;
    }
}
