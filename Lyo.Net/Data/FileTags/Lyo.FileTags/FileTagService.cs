using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.FileTags;

/// <summary>First-match dispatcher over registered readers and writers. Register providers before this service is resolved.</summary>
public sealed class FileTagService : IFileTagService
{
    private readonly IReadOnlyList<IFileTagReader> _readers;
    private readonly IReadOnlyList<IFileTagWriter> _writers;

    /// <summary>Builds a dispatcher. Registration order is the match order.</summary>
    public FileTagService(IEnumerable<IFileTagReader> readers, IEnumerable<IFileTagWriter> writers)
    {
        ArgumentHelpers.ThrowIfNull(readers);
        ArgumentHelpers.ThrowIfNull(writers);
        _readers = [.. readers];
        _writers = [.. writers];
    }

    /// <inheritdoc />
    public bool CanRead(string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        return _readers.Any(reader => reader.CanRead(path));
    }

    /// <inheritdoc />
    public bool CanWrite(string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        return _writers.Any(writer => writer.CanWrite(path));
    }

    /// <inheritdoc />
    public async Task<Result<FileTags>> ReadAsync(string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        var reader = _readers.FirstOrDefault(candidate => candidate.CanRead(path));
        if (reader == null)
            return Result<FileTags>.Failure("No file tag provider can read this file.", "FileTagProviderMissing");

        return await reader.ReadAsync(path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<FileTags>> WriteAsync(string path, FileTagWriteRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(request);
        var writer = _writers.FirstOrDefault(candidate => candidate.CanWrite(path));
        if (writer == null)
            return Result<FileTags>.Failure("No file tag provider can write this file.", "FileTagProviderMissing");

        return await writer.WriteAsync(path, request, ct).ConfigureAwait(false);
    }
}