using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Optional response headers for presigned read URLs (S3 response-header overrides and Azure SAS response headers).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PreSignedReadUrlOptions
{
    /// <summary><c>Content-Disposition</c> on the download response, for example <c>attachment; filename="doc.pdf"</c>.</summary>
    public string? ContentDisposition { get; init; }

    /// <summary><c>Content-Type</c> override for the download response. Null uses the cloud default or the stored type.</summary>
    public string? ContentType { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"PreSignedReadUrlOptions: ContentType={ContentType ?? "(none)"}, ContentDisposition={ContentDisposition ?? "(none)"}";
}
