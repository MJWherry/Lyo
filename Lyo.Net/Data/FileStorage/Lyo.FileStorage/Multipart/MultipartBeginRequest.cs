namespace Lyo.FileStorage.Multipart;

public sealed class MultipartBeginRequest
{
    public long? DeclaredContentLength { get; init; }

    public int PartSizeBytes { get; init; } = 8 * 1024 * 1024;

    public bool Compress { get; init; }

    public bool Encrypt { get; init; }

    public string? KeyId { get; init; }

    public string? PathPrefix { get; init; }

    public string? ContentType { get; init; }

    public string? OriginalFileName { get; init; }

    public string? TenantId { get; init; }

    public TimeSpan? SessionTtl { get; init; }
}