using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST <c>stage/begin</c> payload. <see cref="ProviderKind" /> is the storage-backend name (for example <c>AwsS3</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StagedUploadBeginResult(
    Guid StageId,
    string PresignedPutUrl,
    DateTimeOffset UrlExpiresUtc,
    string StorageLocation,
    string ProviderKind,
    IReadOnlyDictionary<string, string>? RequiredPutHeaders = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"StagedUploadBeginResult: StageId={StageId}, ProviderKind={ProviderKind}, StorageLocation={StorageLocation}, UrlExpiresUtc={UrlExpiresUtc:u}";
}
