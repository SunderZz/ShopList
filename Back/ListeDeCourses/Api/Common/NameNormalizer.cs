using System.Text.RegularExpressions;

namespace ListeDeCourses.Api.Common;

public static class NameNormalizer
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string NormalizeDisplayName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return WhitespaceRegex.Replace(value.Trim(), " ");
    }

    public static string ToKey(string value) =>
        NormalizeDisplayName(value).ToUpperInvariant();

    public static string ToExactFlexibleWhitespacePattern(string value)
    {
        var tokens = WhitespaceRegex
            .Split(NormalizeDisplayName(value))
            .Where(token => token.Length > 0)
            .Select(Regex.Escape);

        return @"^\s*" + string.Join(@"\s+", tokens) + @"\s*$";
    }
}
