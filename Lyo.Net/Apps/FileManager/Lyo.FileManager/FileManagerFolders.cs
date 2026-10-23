namespace Lyo.FileManager;

/// <summary>Matches a typed folder prefix against known directories.</summary>
public static class FileManagerFolders
{
    /// <summary>True when <paramref name="folder" /> or its last segment starts with <paramref name="typed" />.</summary>
    public static bool Matches(string folder, string typed)
    {
        if (string.IsNullOrEmpty(typed))
            return true;

        if (folder.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
            return true;

        var slash = folder.LastIndexOf('/');
        var name = slash < 0 ? folder : folder[(slash + 1)..];
        return name.StartsWith(typed, StringComparison.OrdinalIgnoreCase);
    }
}