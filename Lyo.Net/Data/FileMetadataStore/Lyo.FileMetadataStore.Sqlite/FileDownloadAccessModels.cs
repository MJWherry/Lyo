using System.Diagnostics;

namespace Lyo.FileMetadataStore.Sqlite;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CreateFileDownloadAccessLinkRequest(
    Guid FileId,
    DateTime? NotBeforeUtc = null,
    DateTime? ExpiresAtUtc = null,
    DateTime? WindowStartUtc = null,
    DateTime? WindowEndUtc = null,
    int? MaxDownloads = null,
    string? TenantId = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"CreateFileDownloadAccessLinkRequest: FileId={FileId}, ExpiresAtUtc={ExpiresAtUtc?.ToString("u") ?? "(none)"}, MaxDownloads={MaxDownloads?.ToString() ?? "(none)"}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CreateFileDownloadAccessLinkResult(Guid LinkId, string Token, DateTime CreatedUtc, DateTime? ExpiresAtUtc)
{
    /// <inheritdoc />
    public override string ToString() => $"CreateFileDownloadAccessLinkResult: LinkId={LinkId}, CreatedUtc={CreatedUtc:u}, ExpiresAtUtc={ExpiresAtUtc?.ToString("u") ?? "(none)"}";
}

public enum FileDownloadAccessConsumeFailureReason
{
    NotFound = 0,
    Revoked = 1,
    NotYetValid = 2,
    Expired = 3,
    OutsideWindow = 4,
    MaxDownloadsReached = 5,
    LockUnavailable = 6,
    InvalidToken = 7
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ConsumeFileDownloadAccessLinkResult(
    bool IsAllowed,
    Guid? FileId = null,
    Guid? LinkId = null,
    FileDownloadAccessConsumeFailureReason? FailureReason = null,
    int? DownloadCount = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"ConsumeFileDownloadAccessLinkResult: IsAllowed={IsAllowed}, FileId={FileId?.ToString() ?? "(none)"}, FailureReason={FailureReason?.ToString() ?? "(none)"}";
}
