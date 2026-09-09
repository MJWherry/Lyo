namespace Lyo.ContentThreatScan;

/// <summary>Optional metadata for eligibility and audit — no payload bytes.</summary>
public sealed class ContentThreatScanContext
{
    /// <summary>Caller-supplied original filename, when present.</summary>
    public string? OriginalFileName { get; }

    /// <summary>MIME or content-type string, when present.</summary>
    public string? ContentType { get; }

    /// <summary>Optional trace or correlation id.</summary>
    public string? CorrelationId { get; }

    public ContentThreatScanContext(string? originalFileName = null, string? contentType = null, string? correlationId = null)
    {
        OriginalFileName = originalFileName;
        ContentType = contentType;
        CorrelationId = correlationId;
    }
}