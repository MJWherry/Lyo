using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Abstractions;

/// <summary>
/// Zips stored files by id. Remote or encrypted objects are decrypted via <see cref="IFileStorageService.GetFileStreamAsync" />, written under an IO temp session, then zipped
/// on disk so the archive is not held in RAM.
/// </summary>
public interface IFileStorageArchiveService
{
    /// <summary>
    /// Writes <paramref name="entries" /> into a temp directory, zips that tree, and returns a readable zip stream. Disposing the stream drops the temp session.
    /// </summary>
    /// <param name="entries">Files to include. Duplicate ids keep the first zip path.</param>
    /// <param name="fileName">Download name (for example <c>Q1-reports.zip</c>). Sanitized. Starts as <c>files.zip</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Open zip stream. The caller disposes it (ASP.NET <c>Results.File</c> does this after the response).</returns>
    /// <exception cref="FileStorageArchiveLimitException">Too many files, or total uncompressed size is over the options ceiling.</exception>
    /// <exception cref="FileNotFoundException">An id is missing from storage.</exception>
    /// <exception cref="ArgumentException">A zip path is empty, rooted, or contains <c>..</c>.</exception>
    Task<FileStorageArchive> CreateArchiveAsync(IReadOnlyList<FileStorageArchiveEntry> entries, string? fileName = null, CancellationToken ct = default);
}
