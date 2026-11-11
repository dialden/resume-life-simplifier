using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl;

public class RLSDsl
{
    private readonly UseCaseFactory<IRLSDriver> _useCaseFactory;

    public RLSDsl(UseCaseFactory<IRLSDriver> useCaseFactory)
    {
        _useCaseFactory = useCaseFactory;
    }

    public GoToRLS GoToRLS() => _useCaseFactory.Create<GoToRLS>();

    public CreatePosting CreatePosting() => _useCaseFactory.Create<CreatePosting>();

    public PublishPosting PublishPosting() => _useCaseFactory.Create<PublishPosting>();

    public ViewPosting ViewPosting() => _useCaseFactory.Create<ViewPosting>();

    public ViewPublicListing ViewPublicListing() => _useCaseFactory.Create<ViewPublicListing>();

    public ApplyToPosting ApplyToPosting() => _useCaseFactory.Create<ApplyToPosting>();

    public CompleteResumeAnalysis CompleteResumeAnalysis() => _useCaseFactory.Create<CompleteResumeAnalysis>();

    public ListPostings ListPostings() => _useCaseFactory.Create<ListPostings>();
}
