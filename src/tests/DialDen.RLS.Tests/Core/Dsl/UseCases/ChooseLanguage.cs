using DialDen.RLS.Tests.Core.Drivers.RLS;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ChooseLanguage : RLSUseCase<VoidValue, VoidVerification>
{
    private Language _language = Language.English;

    public ChooseLanguage(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public ChooseLanguage To(Language language)
    {
        _language = language;
        return this;
    }

    public override async Task<RLSUseCaseResult<VoidValue, VoidVerification>> Execute()
    {
        var result = await Driver.ChooseLanguage(_language);
        return new RLSUseCaseResult<VoidValue, VoidVerification>(
            result,
            Context,
            (response, ctx) => new VoidVerification(response, ctx));
    }
}
