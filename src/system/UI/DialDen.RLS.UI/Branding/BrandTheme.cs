using MudBlazor;

namespace DialDen.RLS.UI.Branding;

public sealed class BrandTheme
{
    public string Name => "Côté-Jardins inc.";
    public string LogoPath => "/branding/LOGO_COTE_JARDINS_STANDARD.svg";
    public string LogoAltText => Name;
    public string IconPath => "/branding/LOGO_COTE_JARDINS_FLEUR_STANDARD.svg";
    public string Primary => "#1169B2";
    public string PrimaryHover => "#0B568F";
    public string PrimarySoft => "#E7EFF5";
    public string Secondary => "#05426D";
    public string Accent => "#C79A0A";
    public string AccentSoft => "#FFF8E1";
    public string Background => "#F7F9FC";
    public string Surface => "#FFFFFF";
    public string SurfaceAlt => "#F1F5F9";
    public string TextPrimary => "#193A50";
    public string TextSecondary => "#526875";
    public string Border => "#DCE3EA";
    public string SidebarBackground => "#F7F9FC";
    public string SidebarText => "#05426D";
    public string SidebarMuted => "#536B78";
    public string SidebarHover => "#EAF0F5";
    public string SidebarActive => "#E3EDF5";
    public string SidebarActiveText => "#05426D";
    public string TopbarBackground => "#05426D";
    public string TopbarText => "#FFFFFF";
    public string Focus => "#1169B2";
    public string BodyFontFamily => "Inter, sans-serif";
    public string[] BodyFontFamilies => ["Inter", "sans-serif"];
    public string HeadingFontFamily => BodyFontFamily;

    public MudTheme CreateMudTheme() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Primary,
            Secondary = Secondary,
            Background = Background,
            Surface = Surface,
            TextPrimary = TextPrimary,
            TextSecondary = TextSecondary,
            AppbarBackground = TopbarBackground,
            AppbarText = TopbarText
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = BodyFontFamilies },
            Button = new ButtonTypography { FontFamily = BodyFontFamilies }
        }
    };

    public string CssVariables =>
        $"--brand-primary:{Primary};" +
        $"--brand-primary-hover:{PrimaryHover};" +
        $"--brand-primary-soft:{PrimarySoft};" +
        $"--brand-secondary:{Secondary};" +
        $"--brand-accent:{Accent};" +
        $"--brand-accent-soft:{AccentSoft};" +
        $"--brand-background:{Background};" +
        $"--brand-surface:{Surface};" +
        $"--brand-surface-alt:{SurfaceAlt};" +
        $"--brand-text-primary:{TextPrimary};" +
        $"--brand-text-secondary:{TextSecondary};" +
        $"--brand-border:{Border};" +
        $"--brand-sidebar-background:{SidebarBackground};" +
        $"--brand-sidebar-text:{SidebarText};" +
        $"--brand-sidebar-muted:{SidebarMuted};" +
        $"--brand-sidebar-hover:{SidebarHover};" +
        $"--brand-sidebar-active:{SidebarActive};" +
        $"--brand-sidebar-active-text:{SidebarActiveText};" +
        $"--brand-topbar-background:{TopbarBackground};" +
        $"--brand-topbar-text:{TopbarText};" +
        $"--brand-focus:{Focus};" +
        $"--brand-font-body:{BodyFontFamily};" +
        $"--brand-font-heading:{HeadingFontFamily};";

}
