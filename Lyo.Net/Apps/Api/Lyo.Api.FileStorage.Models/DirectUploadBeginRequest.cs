using System.Diagnostics;
using System.Text.Json;

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

    /// <summary>Opaque caller JSON bag. Lyo does not interpret keys. Null when omitted.</summary>
    public JsonElement? Metadata { get; init; }

    /// <summary>How long the presigned PUT URL lives, in hours. Leave unset for the API default.</summary>
    public double? UrlExpirationHours { get; init; }

    /// <summary>
    /// Optional SHA-256 of the bytes the client will PUT, base64 in JSON (32 bytes). S3 signs it into the URL and rejects a mismatched body, and finalize then trusts the stored
    /// checksum instead of downloading the object. Send every header in the begin response's <c>RequiredPutHeaders</c>.
    /// </summary>
    public byte[]? ExpectedSha256 { get; init; }

    /// <summary>Optional MD5 of the bytes the client will PUT, base64 in JSON (16 bytes). Azure Blob returns it as a required <c>Content-MD5</c> PUT header.</summary>
    public byte[]? ExpectedContentMd5 { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadBeginRequest: OriginalFileName={OriginalFileName ?? "(none)"}, PathPrefix={PathPrefix ?? "(none)"}, DeclaredMaxSizeBytes={DeclaredMaxSizeBytes}";
}