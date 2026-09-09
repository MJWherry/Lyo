using Lyo.Exceptions;

namespace Lyo.Common.Core.Pathing;

/// <summary>
/// Scratch spool paths under <see cref="Path.GetTempPath" />, using a single <c>lyo-</c> naming scheme so orphaned files are attributable and sweepable. Use this for
/// short-lived scratch files inside one method; use <c>Lyo.IO.Temp.IIOTempService</c> when a caller needs a tracked, session-scoped temp directory.
/// </summary>
/// <remarks>
/// <para>Every path is unique, so <see cref="FileMode.CreateNew" /> is safe. Pair <see cref="CreatePath(string)" /> with <see cref="TryDelete" /> in a
/// <c>finally</c> block, or prefer <see cref="CreateStream" /> which deletes on close.</para>
/// </remarks>
public static class TempSpool
{
    /// <summary>Prefix on every generated name so Lyo scratch files are identifiable in the OS temp directory.</summary>
    public const string Prefix = "lyo-";

    /// <summary>Default extension used for generated spool files.</summary>
    public const string DefaultExtension = ".tmp";

    /// <summary>Buffer size used by <see cref="CreateStream" />, matching the default async copy buffer used on Lyo IO paths.</summary>
    public const int DefaultBufferSizeBytes = 81920;

    /// <summary>Builds a unique temp path named <c>lyo-{name}-{guid}{extension}</c>. Does not create the file.</summary>
    /// <param name="name">Short scope tag identifying the caller, for example <c>fs-scan</c>. Must be non-empty.</param>
    /// <param name="extension">Extension including the leading dot.</param>
    public static string CreatePath(string name, string extension = DefaultExtension)
        => CreatePath(name, Guid.NewGuid(), extension);

    /// <summary>
    /// Builds a temp path named <c>lyo-{name}-{id}{extension}</c> from a caller-supplied correlation id (file id, session id, stage id). Callers must keep the id
    /// unique across concurrent operations.
    /// </summary>
    /// <param name="name">Short scope tag identifying the caller, for example <c>fs-scan</c>. Must be non-empty.</param>
    /// <param name="id">Correlation id rendered without dashes.</param>
    /// <param name="extension">Extension including the leading dot.</param>
    public static string CreatePath(string name, Guid id, string extension = DefaultExtension)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return Path.Combine(Path.GetTempPath(), $"{Prefix}{name}-{id:N}{extension}");
    }

    /// <summary>
    /// Opens a read/write spool file at <paramref name="path" /> that deletes itself when the returned stream is closed, so no <c>finally</c> cleanup is needed. Deletes a
    /// partially created file when stream construction fails.
    /// </summary>
    /// <param name="path">A path from <see cref="CreatePath(string)" /> that does not yet exist.</param>
    /// <param name="bufferSizeBytes">Stream buffer size.</param>
    public static FileStream CreateStream(string path, int bufferSizeBytes = DefaultBufferSizeBytes)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        try {
            return new(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, bufferSizeBytes, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        }
        catch {
            TryDelete(path);
            throw;
        }
    }

    /// <summary>
    /// Deletes <paramref name="path" /> if it exists, swallowing any IO failure. Safe to call from a <c>finally</c> block. Callers that care about a failed cleanup should log on
    /// a <c>false</c> return value.
    /// </summary>
    /// <param name="path">Path to remove; null or whitespace is a no-op.</param>
    /// <returns><c>true</c> when the file no longer exists after the call.</returns>
    public static bool TryDelete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try {
            if (File.Exists(path))
                File.Delete(path);

            return true;
        }
        catch {
            return false;
        }
    }

    /// <summary>Recursively deletes <paramref name="path" /> if it exists, swallowing IO failures.</summary>
    /// <param name="path">Directory to remove; null or whitespace is a no-op.</param>
    /// <returns><c>true</c> when the directory no longer exists after the call.</returns>
    public static bool TryDeleteDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try {
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            return true;
        }
        catch {
            return false;
        }
    }
}
