namespace Lyo.FileManager;

/// <summary>Where a local server keeps its SQLite file relative to the file folder.</summary>
public static class FileManagerPaths
{
    /// <summary>SQLite file beside <paramref name="rootDirectory" />. <c>/data/photos</c> becomes <c>/data/photos.file-metadata.db</c>.</summary>
    public static string DefaultSqliteFile(string rootDirectory)
    {
        var full = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetDirectoryName(full);
        var name = Path.GetFileName(full);
        return Path.Combine(string.IsNullOrEmpty(parent) ? Path.GetPathRoot(full) ?? "" : parent, name + ".file-metadata.db");
    }

    /// <summary>Folder that holds <see cref="DefaultSqliteFile" />. <c>/data/photos</c> becomes <c>/data</c>.</summary>
    public static string DefaultSqliteDirectory(string rootDirectory)
        => Path.GetDirectoryName(DefaultSqliteFile(rootDirectory)) ?? "";

    /// <summary>File name of <see cref="DefaultSqliteFile" />. <c>/data/photos</c> becomes <c>photos.file-metadata.db</c>.</summary>
    public static string DefaultSqliteFileName(string rootDirectory)
        => Path.GetFileName(DefaultSqliteFile(rootDirectory));

    /// <summary>Full SQLite path from a catalog folder and a file name.</summary>
    public static string CombineSqliteFile(string directory, string fileName)
        => Path.Combine(directory.Trim(), fileName.Trim());

    /// <summary>Error when <paramref name="database" /> is inside the file folder. A <c>Data Source=</c> connection string is accepted. Null when the file sits outside.</summary>
    public static string? RejectDatabaseInsideRoot(string rootDirectory, string database)
    {
        var file = SqliteFile(database);
        if (file.Length == 0)
            return null;
        var root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(file);
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? "The database file must sit outside the file folder." : null;
    }

    private static string SqliteFile(string database)
    {
        var text = database.Trim();
        const string prefix = "Data Source=";
        if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            text = text[prefix.Length..].Trim().Trim('"');
        var semi = text.IndexOf(';');
        return (semi < 0 ? text : text[..semi]).Trim();
    }
}
