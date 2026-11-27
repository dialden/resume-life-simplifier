using DialDen.RLS.Tests.Fixtures;
using Shouldly;
using System.Text.RegularExpressions;

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

    [Fact]
    public async Task FailedScenarioReportsDocumentConversionTiming()
    {
        var error = await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await harness.For(ExternalSystems.Stubbed).Through(Channel.API)
                .Given().Posting().PublishedTo()
                .And().ResumeAnalysis()
                .When().ApplyToPosting().WithWordResume()
                .Then().ShouldSucceed()
                    .And().ApplicationForPosting("posting")
                        .EventuallyHasExtractedContactInformation(email: "incorrect@example.com"));

        Assert.Contains("incorrect@example.com", error.Message);
        var output = TestContext.Current.TestOutputHelper!.Output;
        Assert.Contains("process=libreoffice phase=started", output);
        Assert.Contains("process=libreoffice phase=completed elapsedMs=", output);
        Assert.Contains("process=pdfinfo phase=completed elapsedMs=", output);
        Assert.DoesNotContain("incorrect@example.com", output);
        Assert.DoesNotContain("rls-resume-", output);
    }

    [Fact]
    public async Task FailedScenarioReportsExtractionRetryAttempts()
    {
        var error = await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await harness.For(ExternalSystems.Stubbed).Through(Channel.API)
                .Given().Posting().PublishedTo()
                .When().ApplyToPosting().WithResumePages(21)
                .Then().ShouldSucceed()
                    .And().ApplicationForPosting("posting")
                        .EventuallyHasExtractedContactInformation());

        Assert.Contains("Resume information extraction failed instead of completing", error.Message);
        var output = TestContext.Current.TestOutputHelper!.Output;
        Assert.Contains("extraction attempt=0 phase=started", output);
        Assert.Contains("extraction attempt=1 phase=started", output);
        Assert.Contains("extraction attempt=2 phase=started", output);
        Assert.Contains("extraction attempt=3 phase=started", output);
        Assert.Contains("phase=faulted elapsedMs=", output);
        Assert.Contains("exception=System.IO.InvalidDataException", output);
        Assert.DoesNotContain("rls-resume-", output);
    }

    [Fact]
    public async Task BrowserWaitTimeoutReportsStateAndRequestAgesWithoutPageContent()
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();
        var home = await ui.OpenHomePage();
        await home.GetError();
        var page = home.Page;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/postings/slow*", async route =>
        {
            await release.Task;
            await route.FulfillAsync(new() { Status = 200, Body = "private response" });
        });
        try
        {
            await page.SetContentAsync("""
                <div id="app"><div class="loading-percentage">private page content</div></div>
                <div role="alert" style="display:none">private alert</div>
                """);
            await page.RunAndWaitForRequestAsync(
                () => page.EvaluateAsync("() => { fetch('/postings/slow?private-query'); }"),
                request => request.Url.Contains("/postings/slow", StringComparison.Ordinal));
            page.SetDefaultTimeout(500);

            var error = await Assert.ThrowsAsync<TimeoutException>(() => home.GetError());
            Assert.Contains("Timeout", error.Message);
            var output = ui.Diagnostics.Snapshot();
            Assert.Contains("wait=home", output);
            Assert.Contains("readyState", output);
            Assert.True(output.Contains("loadingVisible=true"), output);
            Assert.Contains("alert count=0 firstVisible=false", output);
            Assert.Contains("alertIncludingHidden count=1 firstVisible=false", output);
            var pending = Regex.Match(output, @"fetch:/postings count=1 oldestMs=(\d+)");
            Assert.True(pending.Success, output);
            Assert.True(long.Parse(pending.Groups[1].Value) >= 400, output);
            Assert.Matches(@"document:/ count=\d+ failed=0 avgMs=\d+ maxMs=\d+", output);
            Assert.DoesNotContain("private page content", output);
            Assert.DoesNotContain("private alert", output);
            Assert.DoesNotContain("private-query", output);
            Assert.DoesNotContain("private response", output);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task PostingWaitTimeoutReportsItsLocatorState()
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();
        var home = await ui.OpenHomePage();
        await home.GetError();
        var posting = await ui.OpenPosting(987654321);
        await posting.ReadPosting();
        await posting.Page.SetContentAsync("<div id=\"app\"></div>");
        posting.Page.SetDefaultTimeout(500);

        await Assert.ThrowsAsync<TimeoutException>(() => posting.ReadPosting());

        var output = ui.Diagnostics.Snapshot();
        Assert.Contains("wait=posting", output);
        Assert.Contains("details count=0 firstVisible=false", output);
    }
}
