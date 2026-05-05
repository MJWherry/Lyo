namespace Lyo.ContentThreatScan;

/// <summary>Optional metadata for eligibility and auditing (no payload).</summary>
public sealed class ContentThreatScanContext
{
    public ContentThreatScanContext(string? originalFileName = null, string? contentType = null, string? correlationId = null)
    {
        OriginalFileName = originalFileName;
        ContentType = contentType;
        CorrelationId = correlationId;
    }

    /// <summary>Original filename supplied by caller, if any.</summary>
    public string? OriginalFileName { get; }

    /// <summary>MIME or content-type string, if any.</summary>
    public string? ContentType { get; }

    /// <summary>Optional trace/correlation id.</summary>
    public string? CorrelationId { get; }
}
