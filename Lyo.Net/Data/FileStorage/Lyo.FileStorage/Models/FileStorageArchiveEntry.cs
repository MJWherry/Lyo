namespace Lyo.FileStorage.Models;

/// <summary>One stored file to include in an archive, with its relative path inside the zip.</summary>
/// <param name="Id">File storage id.</param>
/// <param name="ZipPath">
/// Relative zip path using <c>/</c> separators (e.g. <c>Vol. 01/Ch. 001/001</c>). When null or whitespace, the service uses the stored original file name. When the last
/// segment has no extension, the service appends one from metadata.
/// </param>
public readonly record struct FileStorageArchiveEntry(Guid Id, string? ZipPath = null);
