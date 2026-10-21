using Lyo.Common.Metadata.Records;

namespace Lyo.FileStorage.Policy;

/// <summary>Groups catalog rows that share one on-disk signature.</summary>
internal static class FileSignatureFamilies
{
    /// <summary>True when <paramref name="type" /> is a signature this package can require, including family members the sniffer reports as the container.</summary>
    public static bool HasRequiredSignature(FileTypeInfo type) => type != FileTypeInfo.Unknown && (type.HeaderSignatures.Count > 0 || type.SignatureFamily != null);

    /// <summary>True when the sniffed row is the claimed row, or both sit in one shared-signature family.</summary>
    public static bool Agrees(FileTypeInfo sniffed, FileTypeInfo claimed) => sniffed == claimed || SameFamily(sniffed, claimed);

    /// <summary>True when <paramref name="sniffed" /> is the catalog row named <paramref name="configuredName" />, or they share a signature family.</summary>
    public static bool MatchesConfiguredName(FileTypeInfo sniffed, string configuredName)
    {
        if (sniffed == FileTypeInfo.Unknown || string.IsNullOrWhiteSpace(configuredName))
            return false;

        var configured = FileTypeInfo.All.FirstOrDefault(t => string.Equals(t.Name, configuredName, StringComparison.OrdinalIgnoreCase));
        if (configured == null || configured == FileTypeInfo.Unknown)
            return false;

        return sniffed == configured || SameFamily(sniffed, configured);
    }

    private static bool SameFamily(FileTypeInfo left, FileTypeInfo right)
    {
        var leftRoot = Root(left);
        var rightRoot = Root(right);
        return leftRoot == rightRoot && (left.SignatureFamily != null || right.SignatureFamily != null || left == right);
    }

    private static FileTypeInfo Root(FileTypeInfo type) => type.SignatureFamily ?? type;
}