using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage.Models;

/// <summary>What <see cref="IFileStorageService.BeginDirectUploadAsync" /> returns.</summary>
public sealed class DirectUploadBeginResult
{
    public required Guid FileId { get; init; }

    public required string PresignedPutUrl { get; init; }

    public required DateTimeOffset UrlExpiresUtc { get; init; }

    /// <summary>Exact object key or name. Relative to the bucket or container when that applies.</summary>
    public required string StorageLocation { get; init; }

    /// <summary>Extra headers the client must send on the PUT when the backend requires them.</summary>
    public IReadOnlyDictionary<string, string>? RequiredPutHeaders { get; init; }
}