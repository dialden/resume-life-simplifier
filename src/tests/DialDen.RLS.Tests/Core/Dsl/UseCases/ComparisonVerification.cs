using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ComparisonVerification(ComparisonResponse response, UseCaseContext context)
    : ResponseVerification<ComparisonResponse>(response, context)
{
    public void ShowsEducationEvidence(string requirement, string? evidence, IReadOnlyList<string> names)
    {
        Response.EducationRequirement.ShouldBe(requirement);
        Response.Candidates.Select(candidate => candidate.Name).Order().ShouldBe(names.Order());
        foreach (var candidate in Response.Candidates)
            candidate.EducationEvidence.ShouldBe(evidence);
    }
}
