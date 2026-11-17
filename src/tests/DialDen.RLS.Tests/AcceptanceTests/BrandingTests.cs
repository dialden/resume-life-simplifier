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
        (await homePage.ReadBrandLogoPath()).ShouldBe("/branding/LOGO_COTE_JARDINS_STANDARD.svg");
        (await homePage.ReadBrandLogoAltText()).ShouldBe("Côté-Jardins inc.");
        (await homePage.ReadFaviconPath()).ShouldBe("/branding/LOGO_COTE_JARDINS_FLEUR_STANDARD.svg");
        (await homePage.ReadSidebarBackgroundColor()).ShouldBe("rgb(247, 249, 252)");
        (await homePage.ReadPrimaryButtonBackgroundColor()).ShouldBe("rgb(17, 105, 178)");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task UsesBrandFontForButtons()
    {
        await using var interaction = _testEnvironment.InteractWithUI();
        var homePage = await interaction.OpenHomePage();

        (await homePage.ReadPrimaryButtonFontFamily()).ShouldBe("Inter, sans-serif");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task LoadsBundledInterForLatinAndLatinExtendedText()
    {
        await using var interaction = _testEnvironment.InteractWithUI();
        var homePage = await interaction.OpenHomePage();

        (await homePage.LoadInterFontsFor("Côté-Jardins")).ShouldBe(1);
        (await homePage.LoadInterFontsFor("Ā")).ShouldBe(1);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ShowsSpinningBackdropFlowerBeforeApplicationStarts()
    {
        await using var interaction = _testEnvironment.InteractWithUI();

        (await interaction.ReadLoadingLogoPathBeforeBlazorStarts())
            .ShouldBe("/branding/jardins-hsl-mark.svg");
        (await interaction.ReadLoadingLogoAnimationNameBeforeBlazorStarts()).ShouldBe("loading-flower-spin");
        (await interaction.ReadLoadingScreenBackgroundColorBeforeBlazorStarts()).ShouldBe("rgb(255, 255, 255)");
        (await interaction.ReadLoadingLogoFilterBeforeBlazorStarts()).ShouldBe("brightness(0.35)");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ShowsLoadingPercentageBeforeApplicationStarts()
    {
        await using var interaction = _testEnvironment.InteractWithUI();
        await interaction.ReadLoadingLogoPathBeforeBlazorStarts();

        (await interaction.ReadLoadingPercentageBeforeBlazorStarts()).ShouldBe("0%");
        (await interaction.ReadLoadingPercentageBeforeBlazorStarts(47)).ShouldBe("47%");
        (await interaction.ReadLoadingPercentageBeforeBlazorStarts(100)).ShouldBe("100%");
    }
}
