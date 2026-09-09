namespace Lyo.FileStorage.Multipart;

public sealed class CompleteMultipartUploadRequest
{
    public required Guid SessionId { get; init; }

    public required IReadOnlyList<CompletedPart> Parts { get; init; }
}