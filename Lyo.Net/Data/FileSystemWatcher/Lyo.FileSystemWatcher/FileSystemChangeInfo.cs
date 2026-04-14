using System.Diagnostics;
using Lyo.FileSystemWatcher.Enums;

namespace Lyo.FileSystemWatcher;

/// <summary>Represents information about a file system change event.</summary>
/// <param name="OldPath">The previous path of the file or directory. Null for created items.</param>
/// <param name="NewPath">The new path of the file or directory. Null for deleted items.</param>
/// <param name="ChangeType">The type of change that occurred.</param>
/// <param name="IsDirectory">True if the change affects a directory; false if it affects a file.</param>
/// <param name="OldFileCount">For directory changes, the number of files in the directory before the change. Null for file changes.</param>
/// <param name="OldDirectoryCount">For directory changes, the number of subdirectories before the change. Null for file changes.</param>
/// <param name="NewFileCount">For directory changes, the number of files in the directory after the change. Null for file changes.</param>
/// <param name="NewDirCount">For directory changes, the number of subdirectories after the change. Null for file changes.</param>
/// <remarks>
/// <para>
/// For file changes, only OldPath, NewPath, ChangeType, and IsDirectory are populated. For directory changes, the file and directory counts are also provided to show how the
/// directory's content changed.
/// </para>
/// <para>
/// Examples:
/// <list type="bullet">
/// <item>File created: OldPath=null, NewPath="C:\folder\file.txt", ChangeType=Created, IsDirectory=false</item>
/// <item>File moved: OldPath="C:\folder1\file.txt", NewPath="C:\folder2\file.txt", ChangeType=Moved, IsDirectory=false</item>
/// <item>Directory changed: OldPath="C:\folder", NewPath="C:\folder", ChangeType=Changed, IsDirectory=true, OldFileCount=5, NewFileCount=6</item>
/// </list>
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileSystemChangeInfo(
    string? OldPath,
    string? NewPath,
    ChangeTypeEnum ChangeType,
    bool IsDirectory,
    int? OldFileCount = null,
    int? OldDirectoryCount = null,
    int? NewFileCount = null,
    int? NewDirCount = null)
{
    public override string ToString()
    {
        if (!IsDirectory)
            return $"{ChangeType} File: Old={OldPath} | New={NewPath}";

        var oldInfo = OldDirectoryCount.HasValue && OldFileCount.HasValue ? $" (was {OldDirectoryCount} directories, {OldFileCount} files)" : "";
        var newInfo = NewDirCount.HasValue && NewFileCount.HasValue ? $" (now {NewDirCount} directories, {NewFileCount} files)" : "";
        return $"{ChangeType} Directory: Old={OldPath} | New={NewPath}{oldInfo}{newInfo}";
    }
}