namespace Lyo.FileStorage.Models;

/// <summary>Optional response headers for presigned read URLs (S3 response header overrides / Azure SAS response headers).</summary>
public sealed record PreSignedReadUrlOptions
{
    /// <summary><c>Content-Disposition</c> header for the download response (e.g. <c>attachment; filename="doc.pdf"</c>).</summary>
    public string? ContentDisposition { get; init; }

    /// <summary><c>Content-Type</c> override for the download response. When null, cloud default / stored type applies.</summary>
    public string? ContentType { get; init; }
}