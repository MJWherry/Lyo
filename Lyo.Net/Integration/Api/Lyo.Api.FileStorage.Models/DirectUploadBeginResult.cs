using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST <c>direct-upload/begin</c> payload.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadBeginResult(
    Guid FileId,
    string PresignedPutUrl,
    DateTimeOffset UrlExpiresUtc,
    string StorageLocation,
    IReadOnlyDictionary<string, string>? RequiredPutHeaders = null)
{
    /// <inheritdoc />
    public override string ToString() => $"DirectUploadBeginResult: FileId={FileId}, StorageLocation={StorageLocation}, UrlExpiresUtc={UrlExpiresUtc:u}";
}
