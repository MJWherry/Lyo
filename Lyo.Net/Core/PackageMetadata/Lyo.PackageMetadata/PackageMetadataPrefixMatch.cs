namespace Lyo.PackageMetadata;

internal static class PackageMetadataPrefixMatch
{
    /// <summary>Trims, appends a trailing dot, and drops blanks and duplicates. Order of first occurrence is kept.</summary>
    internal static List<string> NormalizePrefixes(IReadOnlyList<string>? namespacePrefixes)
    {
        var list = new List<string>();
        if (namespacePrefixes is null || namespacePrefixes.Count == 0)
            return list;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < namespacePrefixes.Count; i++) {
            var raw = namespacePrefixes[i];
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var prefix = raw.Trim();
            if (!prefix.EndsWith(".", StringComparison.Ordinal))
                prefix += ".";

            if (seen.Add(prefix))
                list.Add(prefix);
        }

        return list;
    }

    internal static PackageMetadata? MatchLongest(IReadOnlyList<(string Prefix, PackageMetadata Meta)> orderedLongestFirst, string strippedMethodPrefix)
    {
        for (var i = 0; i < orderedLongestFirst.Count; i++) {
            var (prefix, meta) = orderedLongestFirst[i];
            if (strippedMethodPrefix.StartsWith(prefix, StringComparison.Ordinal))
                return meta;
        }

        return null;
    }
}