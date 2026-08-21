using System.Diagnostics;

namespace Lyo.FileStorage.Multipart;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MultipartBeginResult(Guid SessionId, Guid TargetFileId, int PartSizeBytes, DateTime ExpiresUtc, MultipartUploadProviderKind ProviderKind)
{
    /// <inheritdoc />
    public override string ToString()
        => $"MultipartBeginResult: SessionId={SessionId}, TargetFileId={TargetFileId}, PartSizeBytes={PartSizeBytes}, ProviderKind={ProviderKind}";
}
