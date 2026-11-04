namespace DialDen.RLS.UI.Localization;

/// <summary>The languages the workspace is offered in.</summary>
public static class RLSLanguages
{
    public const string French = "fr";
    public const string English = "en";

    /// <summary>The browser storage key remembering the language the customer chose.</summary>
    public const string StorageKey = "rls.language";

    public static IReadOnlyList<RLSLanguage> All { get; } =
    [
        new(French, "Français"),
        new(English, "English")
    ];

    /// <summary>French unless the customer chose another available language.</summary>
    public static string Resolve(string? chosen) => chosen == English ? English : French;
}
