using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>multipart/begin</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BeginMultipartRequest(
    int PartSizeBytes = 8 * 1024 * 1024,
    bool Compress = false,
    bool Encrypt = false,
    string? KeyId = null,
    string? PathPrefix = null,
    string? ContentType = null,
    string? OriginalFileName = null,
    string? TenantId = null,
    long? DeclaredContentLength = null,
    double? SessionTtlHours = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"BeginMultipartRequest: OriginalFileName={OriginalFileName ?? "(none)"}, PartSizeBytes={PartSizeBytes}, compress={Compress}, encrypt={Encrypt}";
}
