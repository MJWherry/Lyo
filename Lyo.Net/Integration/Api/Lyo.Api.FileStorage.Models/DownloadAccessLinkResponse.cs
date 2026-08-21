using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>Access-link create response. URLs are API-relative paths.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DownloadAccessLinkResponse(Guid LinkId, string Token, string DownloadUrl, string PresignedReadUrl, DateTime CreatedUtc, DateTime? ExpiresAtUtc)
{
    /// <inheritdoc />
    public override string ToString() => $"DownloadAccessLinkResponse: LinkId={LinkId}, CreatedUtc={CreatedUtc:u}, ExpiresAtUtc={ExpiresAtUtc?.ToString("u") ?? "(none)"}";
}
