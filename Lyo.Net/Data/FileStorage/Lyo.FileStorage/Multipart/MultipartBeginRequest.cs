namespace Lyo.FileStorage.Multipart;

public sealed class MultipartBeginRequest
{
    public long? DeclaredContentLength { get; init; }

    /// <summary>
    /// Part size in bytes for the upload. Defaults to 8 MiB which clears the S3 5 MiB minimum and is well within Azure block size limits. Recommended range is 8–16 MiB for
    /// balanced throughput vs. retry cost; cloud backends enforce their own min/max during <c>BeginAsync</c>.
    /// </summary>
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