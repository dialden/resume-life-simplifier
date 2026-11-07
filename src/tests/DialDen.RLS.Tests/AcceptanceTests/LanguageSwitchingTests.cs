using DialDen.RLS.Tests.Fixtures;
using Shouldly;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class LanguageSwitchingTests
{
    private readonly TestEnvironment _testEnvironment;

    public LanguageSwitchingTests(TestEnvironments testEnvironments)
    {
        _testEnvironment = testEnvironments.For(ExternalSystems.Stubbed);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task CanSwitchBetweenFrenchAndEnglishWithoutReloading()
    {
        var homePage = await _testEnvironment.InteractWithUI().OpenHomePage();

        await homePage.SelectLanguage("en");
        (await homePage.ReadWorkspaceTitle()).ShouldBe("Your career workspace");
        (await homePage.ReadDocumentLanguage()).ShouldBe("en");

        await homePage.SelectLanguage("fr");
        (await homePage.ReadWorkspaceTitle()).ShouldBe("Votre espace carrière");
        (await homePage.ReadDocumentLanguage()).ShouldBe("fr");
    }
}
