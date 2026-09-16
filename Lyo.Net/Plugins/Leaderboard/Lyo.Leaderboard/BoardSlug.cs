using System.Text.RegularExpressions;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;

namespace Lyo.Leaderboard;

/// <summary>Builds a URL-safe board slug from a display name.</summary>
public static class BoardSlug
{
    private static readonly Regex NonSlug = new("[^a-z0-9]+", RegexOptions.CultureInvariant);

    /// <summary>Lowercases <paramref name="name" />, replaces runs of non-alphanumerics with <c>-</c>, and appends a short unique suffix.</summary>
    public static string FromName(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        var folded = name.Trim().ToLowerInvariant();
        var slug = NonSlug.Replace(folded, "-").Trim('-');
        if (slug.Length == 0)
            slug = "board";

        if (slug.Length > 48)
            slug = slug[..48].TrimEnd('-');

        return $"{slug}-{NanoId.Create(6).ToLowerInvariant()}";
    }

    /// <summary>Returns true when <paramref name="key" /> matches <c>[a-z][a-z0-9_]*</c>.</summary>
    public static bool IsPropertyKey(string key)
    {
        ArgumentHelpers.ThrowIfNull(key);
        if (key.Length == 0 || key[0] is < 'a' or > 'z')
            return false;

        for (var i = 1; i < key.Length; i++) {
            var c = key[i];
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_')
                continue;

            return false;
        }

        return true;
    }
}
