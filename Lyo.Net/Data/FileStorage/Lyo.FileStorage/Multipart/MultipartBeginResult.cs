namespace Lyo.FileStorage.Multipart;

public sealed record MultipartBeginResult(Guid SessionId, Guid TargetFileId, int PartSizeBytes, DateTime ExpiresUtc, MultipartUploadProviderKind ProviderKind);