using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST <c>multipart/begin</c> payload. <see cref="ProviderKind" /> is the storage-backend name (for example <c>AwsS3</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MultipartBeginResponse(Guid SessionId, Guid TargetFileId, int PartSizeBytes, DateTime ExpiresUtc, string ProviderKind)
{
    /// <inheritdoc />
    public override string ToString()
        => $"MultipartBeginResponse: SessionId={SessionId}, TargetFileId={TargetFileId}, PartSizeBytes={PartSizeBytes}, ProviderKind={ProviderKind}";
}
