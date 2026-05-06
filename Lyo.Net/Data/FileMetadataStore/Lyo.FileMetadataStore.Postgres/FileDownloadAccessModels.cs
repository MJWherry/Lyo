namespace Lyo.FileMetadataStore.Postgres;

public sealed record CreateFileDownloadAccessLinkRequest(
    Guid FileId,
    DateTime? NotBeforeUtc = null,
    DateTime? ExpiresAtUtc = null,
    DateTime? WindowStartUtc = null,
    DateTime? WindowEndUtc = null,
    int? MaxDownloads = null,
    string? TenantId = null);

public sealed record CreateFileDownloadAccessLinkResult(
    Guid LinkId,
    string Token,
    DateTime CreatedUtc,
    DateTime? ExpiresAtUtc);

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

public sealed record ConsumeFileDownloadAccessLinkResult(
    bool IsAllowed,
    Guid? FileId = null,
    Guid? LinkId = null,
    FileDownloadAccessConsumeFailureReason? FailureReason = null,
    int? DownloadCount = null);
