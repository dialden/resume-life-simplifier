using System.Text.RegularExpressions;

namespace DialDen.RLS.Pipelines;

public static class ImageVersion
{
    public static string Validate(string? version)
    {
        if (version is null || !Regex.IsMatch(version, @"\A\w[\w.-]{0,127}\z", RegexOptions.ECMAScript))
        {
            throw new ArgumentException("Supply --version as a valid Docker tag.");
        }
        return version;
    }
}
