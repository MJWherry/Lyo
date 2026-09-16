using System.Diagnostics;
using System.Text.Json;

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

    /// <summary>Opaque caller JSON bag. Lyo does not interpret keys. Null when omitted.</summary>
    public JsonElement? Metadata { get; init; }

    /// <summary>How long the URL stays valid. Starts at one hour when unset. Cannot exceed <see cref="FileStorageServiceBaseOptions.PendingDirectUploadTtl" />.</summary>
    public TimeSpan? UrlExpiration { get; init; }

    /// <summary>
    /// Optional SHA-256 (32 bytes) of the exact bytes the client will PUT. S3 signs it into the URL as <c>x-amz-checksum-sha256</c> and rejects a body that does not
    /// match, so finalize needs no download. Backends that hash at finalize compare it only when their configured <c>HashAlgorithm</c> is SHA-256. A mismatch leaves
    /// the upload pending.
    /// </summary>
    public byte[]? ExpectedSha256 { get; init; }

    /// <summary>
    /// Optional MD5 (16 bytes) of the exact bytes the client will PUT. Azure Blob returns it as a required <c>Content-MD5</c> header, validates the body on write, and
    /// reports the stored digest at finalize. Ignored by backends that verify SHA-256 instead.
    /// </summary>
    public byte[]? ExpectedContentMd5 { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadBeginRequest: OriginalFileName={OriginalFileName ?? "(none)"}, PathPrefix={PathPrefix ?? "(none)"}, DeclaredMaxSizeBytes={DeclaredMaxSizeBytes}";
}
