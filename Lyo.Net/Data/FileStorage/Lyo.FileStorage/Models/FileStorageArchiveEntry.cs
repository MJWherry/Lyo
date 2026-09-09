using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>One stored file to put in an archive, plus its relative path inside the zip.</summary>
/// <param name="Id">File storage id.</param>
/// <param name="ZipPath">
/// Relative zip path using <c>/</c> separators, for example <c>Vol. 01/Ch. 001/001</c>. Null or whitespace uses the stored original file name. If the last segment has no
/// extension, the service appends one from metadata.
/// </param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FileStorageArchiveEntry(Guid Id, string? ZipPath = null)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStorageArchiveEntry: Id={Id}, ZipPath={ZipPath ?? "(default)"}";
}
