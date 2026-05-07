namespace Lyo.PackageMetadata;

internal static class PackageMetadataPrefixMatch
{
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