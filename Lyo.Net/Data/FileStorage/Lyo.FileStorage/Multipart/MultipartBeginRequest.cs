using System.Text.Json;

namespace Lyo.FileStorage.Multipart;

public sealed class MultipartBeginRequest
{
    public long? DeclaredContentLength { get; init; }

    /// <summary>
    /// Part size in bytes. Starts as 8 MiB, which clears the S3 5 MiB floor and sits well inside Azure block-size limits. 8–16 MiB balances throughput against retry cost. Cloud
    /// backends apply their own min/max during <c>BeginAsync</c>.
    /// </summary>
    public int PartSizeBytes { get; init; } = 8 * 1024 * 1024;

    public bool Compress { get; init; }

    public bool Encrypt { get; init; }

    public string? KeyId { get; init; }

    public string? PathPrefix { get; init; }

    public string? ContentType { get; init; }

    /// <summary>Optional client-declared character encoding of the plaintext (IANA/web name). Stored as given after trim. Not converted.</summary>
    public string? Charset { get; init; }

    public string? OriginalFileName { get; init; }

    public string? TenantId { get; init; }

    /// <summary>Opaque caller JSON bag. Lyo does not interpret keys. Null when omitted.</summary>
    public JsonElement? Metadata { get; init; }

    public TimeSpan? SessionTtl { get; init; }
}