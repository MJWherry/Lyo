using Lyo.Exceptions;

namespace Lyo.FileManager;

/// <summary>Files and folders that a drop into one folder should move or copy.</summary>
public sealed class FileManagerDropPlan
{
    /// <summary>Folder the items are dropped into. Null is the root.</summary>
    public string? Destination { get; }

    /// <summary>Directories to nest under <see cref="Destination" />.</summary>
    public IReadOnlyList<FileManagerEntry> Folders { get; }

    /// <summary>Catalog files to place on <see cref="Destination" />.</summary>
    public IReadOnlyList<FileManagerEntry> Files { get; }

    /// <summary>Physical objects to place on <see cref="Destination" />.</summary>
    public IReadOnlyList<FileManagerEntry> Physical { get; }

    /// <summary>Folder names that cannot move because the target is the folder or a child of it.</summary>
    public IReadOnlyList<string> Rejected { get; }

    /// <summary>True when at least one item can move or copy.</summary>
    public bool HasWork => Folders.Count > 0 || Files.Count > 0 || Physical.Count > 0;

    /// <summary>Builds a plan. <paramref name="destination" /> null is the root.</summary>
    public FileManagerDropPlan(
        string? destination,
        IReadOnlyList<FileManagerEntry> folders,
        IReadOnlyList<FileManagerEntry> files,
        IReadOnlyList<FileManagerEntry> physical,
        IReadOnlyList<string> rejected)
    {
        Destination = destination;
        Folders = folders;
        Files = files;
        Physical = physical;
        Rejected = rejected;
    }

    /// <summary>Prefix <paramref name="folder" /> occupies after it is placed under <see cref="Destination" />.</summary>
    public string FolderPrefix(FileManagerEntry folder)
    {
        ArgumentHelpers.ThrowIfNull(folder);
        return Join(Destination, folder.Name);
    }

    internal static string Join(string? destination, string name)
    {
        var segment = name.Trim().Trim('/');
        var parent = destination?.Trim().Trim('/') ?? "";
        return parent.Length == 0 ? segment : parent + "/" + segment;
    }
}

/// <summary>Decides where a drag of files and folders lands. The window performs the move.</summary>
public static class FileManagerDrop
{
    /// <summary>Folder a drop lands in. A directory row is that folder. A file row is the folder that contains the file. No row uses <paramref name="openPrefix" />.</summary>
    public static string? Target(FileManagerEntry? hovered, string? openPrefix) => Normalize(hovered == null ? openPrefix : hovered.PathPrefix);

    /// <summary>
    /// Splits <paramref name="entries" /> for a drop into <paramref name="destination" />. A folder dropped onto itself or a descendant is rejected. An item that already lives
    /// there is left out.
    /// </summary>
    public static FileManagerDropPlan Plan(IReadOnlyList<FileManagerEntry> entries, string? destination)
    {
        ArgumentHelpers.ThrowIfNull(entries);
        var dest = Normalize(destination);
        List<FileManagerEntry> folders = [];
        List<FileManagerEntry> files = [];
        List<FileManagerEntry> physical = [];
        List<string> rejected = [];
        foreach (var entry in entries) {
            if (entry.IsDirectory) {
                if (Contains(entry.PathPrefix, dest)) {
                    rejected.Add(entry.Name);
                    continue;
                }

                if (Same(FileManagerDropPlan.Join(dest, entry.Name), entry.PathPrefix))
                    continue;

                folders.Add(entry);
                continue;
            }

            if (Same(entry.PathPrefix, dest))
                continue;

            if (entry.FileId != null)
                files.Add(entry);
            else if (!string.IsNullOrWhiteSpace(entry.PhysicalKey))
                physical.Add(entry);
        }

        return new(dest, folders, files, physical, rejected);
    }

    private static bool Contains(string? folderPrefix, string? target)
    {
        var folder = Trim(folderPrefix);
        if (folder.Length == 0)
            return true;

        var dest = Trim(target);
        return dest.Equals(folder, StringComparison.Ordinal) || dest.StartsWith(folder + "/", StringComparison.Ordinal);
    }

    private static bool Same(string? left, string? right) => Trim(left).Equals(Trim(right), StringComparison.Ordinal);

    private static string? Normalize(string? value)
    {
        var trimmed = Trim(value);
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string Trim(string? value) => value?.Trim().Trim('/') ?? "";
}