namespace Lyo.FileMetadataStore;

/// <summary>PathPrefix matching used by <see cref="IFileMetadataStore.ListByPathPrefixAsync" /> and FileStorage folder listing.</summary>
public static class FileMetadataPathPrefix
{
    /// <summary>Null when <paramref name="pathPrefix" /> is missing or whitespace; otherwise trimmed of slashes.</summary>
    public static string? Normalize(string? pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix))
            return null;

        return pathPrefix.Trim().Trim('/');
    }

    /// <summary>True when <paramref name="storedPrefix" /> is an immediate file of <paramref name="parentPrefix" />.</summary>
    public static bool IsImmediate(string? parentPrefix, string? storedPrefix)
        => string.Equals(Normalize(parentPrefix), Normalize(storedPrefix), StringComparison.Ordinal);

    /// <summary>True when <paramref name="storedPrefix" /> is under <paramref name="parentPrefix" />, including the parent itself.</summary>
    public static bool IsUnder(string? parentPrefix, string? storedPrefix, bool includeDescendants)
    {
        if (!includeDescendants)
            return IsImmediate(parentPrefix, storedPrefix);

        var parent = Normalize(parentPrefix);
        var stored = Normalize(storedPrefix);
        if (parent == null)
            return true;

        return string.Equals(stored, parent, StringComparison.Ordinal)
               || (stored != null && stored.StartsWith(parent + "/", StringComparison.Ordinal));
    }

    /// <summary>Next folder segment under <paramref name="parentPrefix" />, or null when <paramref name="descendantPrefix" /> is not a nested prefix.</summary>
    public static string? ImmediateChildFolder(string? parentPrefix, string? descendantPrefix)
    {
        var parent = Normalize(parentPrefix);
        var desc = Normalize(descendantPrefix);
        if (desc == null)
            return null;

        if (parent == null) {
            var slash = desc.IndexOf('/');
            return slash < 0 ? desc : desc[..slash];
        }

        var needle = parent + "/";
        if (!desc.StartsWith(needle, StringComparison.Ordinal))
            return null;

        var rest = desc[needle.Length..];
        if (rest.Length == 0)
            return null;

        var nextSlash = rest.IndexOf('/');
        return nextSlash < 0 ? rest : rest[..nextSlash];
    }
}
