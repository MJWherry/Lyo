namespace Lyo.PackageMetadata;

/// <summary>Parse SPDX license expressions into <see cref="SpdxLicenseExpressionSyntax" /> (preserves <c>AND</c>, <c>OR</c>, <c>WITH</c>).</summary>
public static class PackageLicenseExpression
{
    /// <summary>Parses <paramref name="licenseExpression" /> when it matches a supported SPDX subset, or returns <see langword="null" />.</summary>
    public static SpdxLicenseExpressionSyntax? TryParseSyntax(string? licenseExpression) =>
        SpdxLicenseExpressionParser.TryParse(licenseExpression);

    /// <summary>All distinct license and exception identifiers in <paramref name="licenseExpression" /> (sorted); <see langword="null" /> if parse fails.</summary>
    public static IReadOnlyList<string>? TryGetSpdxLicenseIdentifiers(string? licenseExpression)
    {
        var root = TryParseSyntax(licenseExpression);
        if (root is null)
            return null;
        var set = new HashSet<string>(StringComparer.Ordinal);
        CollectIdentifiers(root, set);
        if (set.Count == 0)
            return null;
        var list = set.ToList();
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    private static void CollectIdentifiers(SpdxLicenseExpressionSyntax node, HashSet<string> into)
    {
        switch (node.Kind) {
            case "license":
            case "exception":
                if (!string.IsNullOrEmpty(node.Identifier))
                    into.Add(node.Identifier!);
                break;
            case "and":
            case "or":
                if (node.Left is not null)
                    CollectIdentifiers(node.Left, into);
                if (node.Right is not null)
                    CollectIdentifiers(node.Right, into);
                break;
            case "with":
                if (node.InnerLicense is not null)
                    CollectIdentifiers(node.InnerLicense, into);
                if (node.InnerException is not null)
                    CollectIdentifiers(node.InnerException, into);
                break;
        }
    }
}
