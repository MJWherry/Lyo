using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Abstractions;

/// <summary>
/// Builds a zip of stored files by id. Remote/encrypted objects are decrypted through <see cref="IFileStorageService.GetFileStreamAsync" />, spooled under an IO temp session,
/// then zipped on disk so the payload is not held in RAM.
/// </summary>
public interface IFileStorageArchiveService
{
    /// <summary>
    /// Spools <paramref name="entries" /> into a temp directory, zips that tree, and returns a readable stream of the zip. Disposing the stream deletes the temp session.
    /// </summary>
    /// <param name="entries">Files to include. Duplicate ids keep the first zip path.</param>
    /// <param name="fileName">Download file name (e.g. <c>Q1-reports.zip</c>). Sanitized; defaults to <c>files.zip</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An open zip stream. The caller must dispose it (ASP.NET <c>Results.File</c> does this after the response).</returns>
    /// <exception cref="FileStorageArchiveLimitException">Too many files or total uncompressed size exceeds options.</exception>
    /// <exception cref="FileNotFoundException">An id is missing from storage.</exception>
    /// <exception cref="ArgumentException">A zip path is empty, rooted, or contains <c>..</c>.</exception>
    Task<FileStorageArchive> CreateArchiveAsync(IReadOnlyList<FileStorageArchiveEntry> entries, string? fileName = null, CancellationToken ct = default);
}
