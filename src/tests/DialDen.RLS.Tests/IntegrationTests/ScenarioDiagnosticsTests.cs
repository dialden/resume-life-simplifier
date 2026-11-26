using DialDen.RLS.Tests.Fixtures;
using Shouldly;

namespace DialDen.RLS.Tests.IntegrationTests;

public class ScenarioDiagnosticsTests(TestHarness harness)
{
    [Theory, CombinatorialData]
    public async Task FailedScenariosReportTheirOwnDiagnostics(Channel channel)
    {
        await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await harness.For(ExternalSystems.Stubbed).Through(channel)
                .When().CreatePosting().WithTitle("private test title")
                .Then().ShouldSucceed().And().Posting().HasTitle("wrong title"));

        var output = TestContext.Current.TestOutputHelper!.Output;
        Assert.Contains("Scenario diagnostics", output);
        Assert.Contains("Worker", output);
        Assert.Contains("API", output);
        if (channel == Channel.UI)
            Assert.Contains("Browser", output);
        Assert.DoesNotContain("Consume started:", output);
        Assert.DoesNotContain("private test title", output);
    }

    [Fact]
    public async Task FailedExtractionScenarioReportsMessagingActivity()
    {
        await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await harness.For(ExternalSystems.Stubbed).Through(Channel.API)
                .Given().Posting().PublishedTo()
                .And().ResumeAnalysis()
                .When().ApplyToPosting().WithResume()
                .Then().ShouldSucceed()
                    .And().ApplicationForPosting("posting")
                        .EventuallyHasExtractedContactInformation(email: "incorrect@example.com"));

        var output = TestContext.Current.TestOutputHelper!.Output;
        Assert.Contains("Consume started: DialDen.RLS.Messaging.Contracts.ApplicationReceived", output);
    }
}
