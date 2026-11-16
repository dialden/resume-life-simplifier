using DialDen.RLS.Tests.Fixtures;
using Shouldly;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class BrandingTests
{
    private readonly TestEnvironment _testEnvironment;

    public BrandingTests(TestEnvironments testEnvironments)
    {
        _testEnvironment = testEnvironments.For(ExternalSystems.Stubbed);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task DisplaysConfiguredCompanyBranding()
    {
        await using var interaction = _testEnvironment.InteractWithUI();
        var homePage = await interaction.OpenHomePage();

        (await homePage.GetError()).ShouldBeNull();
        (await homePage.IsBrandLogoVisible()).ShouldBeTrue();
        (await homePage.ReadBrandLogoAltText()).ShouldBe("Les Jardins du Haut-Saint-Laurent");
        (await homePage.ReadFaviconPath()).ShouldBe("/branding/jardins-hsl-mark.svg");
        (await homePage.ReadSidebarBackgroundColor()).ShouldBe("rgb(242, 241, 232)");
        (await homePage.ReadPrimaryButtonBackgroundColor()).ShouldBe("rgb(17, 105, 178)");
    }
}
