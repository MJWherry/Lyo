using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>JSON body posted to <c>direct-upload/begin</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadBeginRequest
{
    /// <summary>Original file name stored on metadata.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Optional prefix used when laying out shards.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Declared size cap used when checking policy.</summary>
    public required long DeclaredMaxSizeBytes { get; init; }

    /// <summary>MIME type written to metadata and used by scans.</summary>
    public string? ContentType { get; init; }

    /// <summary>Optional client-declared character encoding of the plaintext (IANA/web name). Stored as given after trim. Not converted.</summary>
    public string? Charset { get; init; }

    /// <summary>Optional tenant id.</summary>
    public string? TenantId { get; init; }

    /// <summary>How long the presigned PUT URL lives, in hours. Leave unset for the API default.</summary>
    public double? UrlExpirationHours { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadBeginRequest: OriginalFileName={OriginalFileName ?? "(none)"}, PathPrefix={PathPrefix ?? "(none)"}, DeclaredMaxSizeBytes={DeclaredMaxSizeBytes}";
}
