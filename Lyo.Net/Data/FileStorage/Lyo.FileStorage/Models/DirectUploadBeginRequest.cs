namespace Lyo.FileStorage.Models;

/// <summary>Begins a client-side single PUT upload to object/blob storage.</summary>
public sealed record DirectUploadBeginRequest
{
    /// <summary>Original filename hint for metadata (optional).</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Optional path prefix for shard layout.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Declared maximum size hint for policy validation. Required so the policy can enforce a size ceiling at the start of the direct upload.</summary>
    public required long DeclaredMaxSizeBytes { get; init; }

    /// <summary>MIME type for metadata and scans (optional).</summary>
    public string? ContentType { get; init; }

    /// <summary>Optional tenant identifier.</summary>
    public string? TenantId { get; init; }

    /// <summary>URL TTL; default one hour.</summary>
    public TimeSpan? UrlExpiration { get; init; }
}