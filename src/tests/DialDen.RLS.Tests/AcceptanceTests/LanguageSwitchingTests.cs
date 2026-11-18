using DialDen.RLS.Tests.Fixtures;
using Shouldly;

namespace DialDen.RLS.Tests.AcceptanceTests;

public class LanguageSwitchingTests(TestHarness harness)
{
    [Trait("Category", "Acceptance")]
    [Theory]
    [InlineData("/jobs/2147483647")]
    [InlineData("/jobs/2147483647/apply")]
    public async Task PublicJobPagesCanSwitchBetweenFrenchAndEnglish(string path)
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();

        (await ui.ReadPublicPageHeadingInLanguage(path, "en")).ShouldBe("Page not found");
        (await ui.ReadPublicPageHeadingInLanguage(path, "fr")).ShouldBe("Page introuvable");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task CanSwitchBetweenFrenchAndEnglishWithoutReloading()
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();
        var homePage = await ui.OpenHomePage();

        await homePage.SelectLanguage("en");
        (await homePage.ReadWorkspaceTitle()).ShouldBe("Your career workspace");
        (await homePage.ReadDocumentLanguage()).ShouldBe("en");

        await homePage.SelectLanguage("fr");
        (await homePage.ReadWorkspaceTitle()).ShouldBe("Votre espace carrière");
        (await homePage.ReadDocumentLanguage()).ShouldBe("fr");
    }
}
