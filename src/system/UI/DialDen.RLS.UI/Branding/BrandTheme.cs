namespace DialDen.RLS.UI.Branding;

public sealed class BrandTheme
{
    public string Name => "Les Jardins du Haut-Saint-Laurent";
    public string LogoPath => "/branding/jardins-hsl.svg";
    public string LogoAltText => Name;
    public string IconPath => "/branding/jardins-hsl-mark.svg";
    public string Primary => "#1169B2";
    public string PrimaryHover => "#0B568F";
    public string PrimarySoft => "#E7EFF5";
    public string Secondary => "#05426D";
    public string Accent => "#C79A0A";
    public string AccentSoft => "#F3ECD1";
    public string Background => "#F2F1E8";
    public string Surface => "#FAF9F3";
    public string SurfaceAlt => "#E6E4D8";
    public string TextPrimary => "#193A50";
    public string TextSecondary => "#526875";
    public string Border => "#D8D6CA";
    public string SidebarBackground => "#F2F1E8";
    public string SidebarText => "#05426D";
    public string SidebarMuted => "#536B78";
    public string SidebarHover => "#E5E3D8";
    public string SidebarActive => "#DCE7ED";
    public string SidebarActiveText => "#05426D";
    public string TopbarBackground => "#05426D";
    public string TopbarText => "#FFFFFF";
    public string Focus => "#1169B2";
    public string BodyFontFamily => "Inter, \"Segoe UI\", Arial, sans-serif";
    public string HeadingFontFamily => BodyFontFamily;

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
