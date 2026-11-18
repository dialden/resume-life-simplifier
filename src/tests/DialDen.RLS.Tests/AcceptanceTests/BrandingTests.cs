using DialDen.RLS.Tests.Fixtures;
using Shouldly;

namespace DialDen.RLS.Tests.AcceptanceTests;

public class BrandingTests(TestHarness harness)
{
    [Trait("Category", "Acceptance")]
    [Theory]
    [InlineData("/jobs/2147483647")]
    [InlineData("/jobs/2147483647/apply")]
    public async Task PublicJobPagesDoNotShowRecruiterWorkspaceNavigation(string path)
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();

        (await ui.ShowsWorkspaceNavigationAt(path)).ShouldBeFalse();
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task DisplaysConfiguredCompanyBranding()
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();
        var homePage = await ui.OpenHomePage();

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
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();
        var homePage = await ui.OpenHomePage();

        (await homePage.ReadPrimaryButtonFontFamily()).ShouldBe("Inter, sans-serif");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task LoadsBundledInterForLatinAndLatinExtendedText()
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();
        var homePage = await ui.OpenHomePage();

        (await homePage.LoadInterFontsFor("Côté-Jardins")).ShouldBe(1);
        (await homePage.LoadInterFontsFor("Ā")).ShouldBe(1);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ShowsSpinningBackdropFlowerBeforeApplicationStarts()
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();

        (await ui.ReadLoadingLogoPathBeforeBlazorStarts())
            .ShouldBe("/branding/jardins-hsl-mark.svg");
        (await ui.ReadLoadingLogoAnimationNameBeforeBlazorStarts()).ShouldBe("loading-flower-spin");
        (await ui.ReadLoadingScreenBackgroundColorBeforeBlazorStarts()).ShouldBe("rgb(255, 255, 255)");
        (await ui.ReadLoadingLogoFilterBeforeBlazorStarts()).ShouldBe("brightness(0.35)");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ShowsLoadingPercentageBeforeApplicationStarts()
    {
        await using var ui = await harness.For(ExternalSystems.Stubbed).ThroughUI();
        await ui.ReadLoadingLogoPathBeforeBlazorStarts();

        (await ui.ReadLoadingPercentageBeforeBlazorStarts()).ShouldBe("0%");
        (await ui.ReadLoadingPercentageBeforeBlazorStarts(47)).ShouldBe("47%");
        (await ui.ReadLoadingPercentageBeforeBlazorStarts(100)).ShouldBe("100%");
    }
}
