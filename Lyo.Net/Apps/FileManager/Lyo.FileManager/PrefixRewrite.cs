namespace Lyo.FileManager;

/// <summary>Rewrites a file prefix when a folder moves or is copied.</summary>
public static class PrefixRewrite
{
    /// <summary>
    /// Maps <paramref name="filePrefix" /> from under <paramref name="sourceFolder" /> onto <paramref name="destinationFolder" />.
    /// <c>docs/a</c> moved to <c>archive</c> becomes <c>archive/a</c>.
    /// </summary>
    public static string? Rewrite(string sourceFolder, string destinationFolder, string? filePrefix)
    {
        var source = Trim(sourceFolder);
        var destination = Trim(destinationFolder);
        var current = Trim(filePrefix);
        if (current.Equals(source, StringComparison.Ordinal))
            return destination.Length == 0 ? null : destination;
        if (current.StartsWith(source + "/", StringComparison.Ordinal)) {
            var rest = current[source.Length..];
            return destination.Length == 0 ? rest.TrimStart('/') : destination + rest;
        }

        return current.Length == 0 ? null : current;
    }

    /// <summary>Parent prefix. Root returns null.</summary>
    public static string? Parent(string? prefix)
    {
        var current = Trim(prefix);
        if (current.Length == 0)
            return null;
        var slash = current.LastIndexOf('/');
        return slash < 0 ? null : current[..slash];
    }

    private static string Trim(string? value) => value?.Trim().Trim('/') ?? "";
}
