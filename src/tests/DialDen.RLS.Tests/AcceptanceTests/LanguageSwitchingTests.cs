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
        await using var interaction = _testEnvironment.InteractWithUI();

        (await interaction.ReadPublicPageHeadingInLanguage(path, "en")).ShouldBe("Page not found");
        (await interaction.ReadPublicPageHeadingInLanguage(path, "fr")).ShouldBe("Page introuvable");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task CanSwitchBetweenFrenchAndEnglishWithoutReloading()
    {
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI, async interaction =>
        {
            var homePage = await interaction.OpenHomePage();

            await homePage.SelectLanguage("en");
            (await homePage.ReadWorkspaceTitle()).ShouldBe("Your career workspace");
            (await homePage.ReadDocumentLanguage()).ShouldBe("en");

            await homePage.SelectLanguage("fr");
            (await homePage.ReadWorkspaceTitle()).ShouldBe("Votre espace carrière");
            (await homePage.ReadDocumentLanguage()).ShouldBe("fr");
        });
    }
}
