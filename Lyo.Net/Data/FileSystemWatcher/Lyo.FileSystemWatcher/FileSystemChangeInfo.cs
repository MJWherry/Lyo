using System.Diagnostics;
using Lyo.FileSystemWatcher.Enums;

namespace Lyo.FileSystemWatcher;

/// <summary>Details of one file-system change event.</summary>
/// <param name="OldPath">Previous path of the file or directory. Null when the item was created.</param>
/// <param name="NewPath">New path of the file or directory. Null when the item was deleted.</param>
/// <param name="ChangeType">Kind of change that happened.</param>
/// <param name="IsDirectory">True when the change is about a directory; false when it is about a file.</param>
/// <param name="OldFileCount">For directory changes, how many files were in the directory before. Null for file changes.</param>
/// <param name="OldDirectoryCount">For directory changes, how many subdirectories existed before. Null for file changes.</param>
/// <param name="NewFileCount">For directory changes, how many files are in the directory after. Null for file changes.</param>
/// <param name="NewDirCount">For directory changes, how many subdirectories exist after. Null for file changes.</param>
/// <remarks>
/// <para>
/// File changes fill OldPath, NewPath, ChangeType, and IsDirectory only. Directory changes also include the file and directory counts so callers can see how the
/// directory contents moved.
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