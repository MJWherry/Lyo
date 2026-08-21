using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>files/{fileId}/access-links</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CreateDownloadAccessLinkRequest(
    DateTime? NotBeforeUtc = null,
    DateTime? ExpiresAtUtc = null,
    DateTime? WindowStartUtc = null,
    DateTime? WindowEndUtc = null,
    int? MaxDownloads = null,
    string? TenantId = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"CreateDownloadAccessLinkRequest: ExpiresAtUtc={ExpiresAtUtc?.ToString("u") ?? "(none)"}, MaxDownloads={MaxDownloads?.ToString() ?? "(none)"}";
}
