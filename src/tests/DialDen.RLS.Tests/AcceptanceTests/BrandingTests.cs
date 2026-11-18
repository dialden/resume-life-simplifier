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
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI, async interaction =>
        {
            (await interaction.ShowsWorkspaceNavigationAt(path)).ShouldBeFalse();
        });
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task DisplaysConfiguredCompanyBranding()
    {
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI, async interaction =>
        {
            var homePage = await interaction.OpenHomePage();

            (await homePage.GetError()).ShouldBeNull();
            (await homePage.IsBrandLogoVisible()).ShouldBeTrue();
            (await homePage.ReadBrandLogoPath()).ShouldBe("/branding/LOGO_COTE_JARDINS_STANDARD.svg");
            (await homePage.ReadBrandLogoAltText()).ShouldBe("Côté-Jardins inc.");
            (await homePage.ReadFaviconPath()).ShouldBe("/branding/LOGO_COTE_JARDINS_FLEUR_STANDARD.svg");
            (await homePage.ReadSidebarBackgroundColor()).ShouldBe("rgb(247, 249, 252)");
            (await homePage.ReadPrimaryButtonBackgroundColor()).ShouldBe("rgb(17, 105, 178)");
        });
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task UsesBrandFontForButtons()
    {
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI, async interaction =>
        {
            var homePage = await interaction.OpenHomePage();
            (await homePage.ReadPrimaryButtonFontFamily()).ShouldBe("Inter, sans-serif");
        });
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task LoadsBundledInterForLatinAndLatinExtendedText()
    {
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI, async interaction =>
        {
            var homePage = await interaction.OpenHomePage();
            (await homePage.LoadInterFontsFor("Côté-Jardins")).ShouldBe(1);
            (await homePage.LoadInterFontsFor("Ā")).ShouldBe(1);
        });
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ShowsSpinningBackdropFlowerBeforeApplicationStarts()
    {
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI, async interaction =>
        {
            (await interaction.ReadLoadingLogoPathBeforeBlazorStarts())
                .ShouldBe("/branding/jardins-hsl-mark.svg");
            (await interaction.ReadLoadingLogoAnimationNameBeforeBlazorStarts()).ShouldBe("loading-flower-spin");
            (await interaction.ReadLoadingScreenBackgroundColorBeforeBlazorStarts()).ShouldBe("rgb(255, 255, 255)");
            (await interaction.ReadLoadingLogoFilterBeforeBlazorStarts()).ShouldBe("brightness(0.35)");
        });
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ShowsLoadingPercentageBeforeApplicationStarts()
    {
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI, async interaction =>
        {
            await interaction.ReadLoadingLogoPathBeforeBlazorStarts();

            (await interaction.ReadLoadingPercentageBeforeBlazorStarts()).ShouldBe("0%");
            (await interaction.ReadLoadingPercentageBeforeBlazorStarts(47)).ShouldBe("47%");
            (await interaction.ReadLoadingPercentageBeforeBlazorStarts(100)).ShouldBe("100%");
        });
    }
}
