using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class CompareCandidates(IRLSDriver driver, UseCaseContext context)
    : RLSUseCase<ComparisonResponse, ComparisonVerification>(driver, context)
{
    private string[] _names = [];
    public CompareCandidates Named(string[] names) { _names = names; return this; }

    public override async Task<RLSUseCaseResult<ComparisonResponse, ComparisonVerification>> Execute()
    {
        var postingId = int.Parse(Context.GetResultValue(Scenario.ScenarioDefaults.PostingId)
            ?? throw new InvalidOperationException("The posting has not been created."), CultureInfo.InvariantCulture);
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        int[] ids;
        while (true)
        {
            var applications = await Driver.ListApplications(postingId);
            if (applications.IsFailure)
                throw new InvalidOperationException($"Could not review applications: {applications.Error}");
            var selected = applications.Value.Where(item => _names.Contains(item.Name)).ToArray();
            if (selected.Length == _names.Length && selected.All(item =>
                    item.ReviewGroup is "Qualified" or "Verify" or "Disqualified"))
            {
                ids = selected.Select(item => item.Id).ToArray();
                break;
            }
            if (elapsed.Elapsed >= TimeSpan.FromSeconds(30))
                throw new InvalidOperationException("The selected candidates were not assessed.");
            await Task.Delay(250);
        }
        var result = await Driver.CompareCandidates(postingId, ids);
        return new(result, Context, (response, ctx) => new ComparisonVerification(response, ctx));
    }
}
