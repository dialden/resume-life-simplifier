using Microsoft.Extensions.Localization;

namespace DialDen.RLS.UI.Localization;

public static class StringLocalizerExtensions
{
    /// <summary>
    /// Translates a value such as a status coming from the API, keeping the value itself
    /// when the workspace has no translation for it.
    /// </summary>
    public static string Translate(this IStringLocalizer localizer, string group, string value)
    {
        var translation = localizer[$"{group}_{value}"];
        return translation.ResourceNotFound ? value : translation.Value;
    }
}
