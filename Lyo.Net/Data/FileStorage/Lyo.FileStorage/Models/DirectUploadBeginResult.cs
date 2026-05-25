using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage.Models;

/// <summary>Result from <see cref="IFileStorageService.BeginDirectUploadAsync" />.</summary>
public sealed class DirectUploadBeginResult
{
    public required Guid FileId { get; init; }

    public required string PresignedPutUrl { get; init; }

    public required DateTimeOffset UrlExpiresUtc { get; init; }

    /// <summary>Exact object key/name (relative within bucket/container when applicable).</summary>
    public required string StorageLocation { get; init; }

    /// <summary>Additional headers the client MUST send with the PUT where required by the backend.</summary>
    public IReadOnlyDictionary<string, string>? RequiredPutHeaders { get; init; }
}
