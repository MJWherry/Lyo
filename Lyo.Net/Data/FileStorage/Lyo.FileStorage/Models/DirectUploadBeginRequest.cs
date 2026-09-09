using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Starts a client-side single PUT upload to object or blob storage.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadBeginRequest
{
    /// <summary>Optional original filename stored on metadata.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Optional path prefix used for shard layout.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Declared size ceiling for policy checks. Required so policy can enforce a max size before the PUT starts.</summary>
    public required long DeclaredMaxSizeBytes { get; init; }

    /// <summary>Optional MIME type used for metadata.</summary>
    public string? ContentType { get; init; }

    /// <summary>Optional client-declared character encoding of the plaintext (IANA/web name). Stored as given after trim. Not converted.</summary>
    public string? Charset { get; init; }

    /// <summary>Optional tenant id.</summary>
    public string? TenantId { get; init; }

    /// <summary>How long the URL stays valid. Starts at one hour when unset.</summary>
    public TimeSpan? UrlExpiration { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadBeginRequest: OriginalFileName={OriginalFileName ?? "(none)"}, PathPrefix={PathPrefix ?? "(none)"}, DeclaredMaxSizeBytes={DeclaredMaxSizeBytes}";
}
