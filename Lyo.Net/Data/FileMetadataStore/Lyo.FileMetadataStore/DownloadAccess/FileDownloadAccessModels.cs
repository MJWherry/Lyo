using System.Diagnostics;

namespace Lyo.FileMetadataStore.DownloadAccess;

/// <summary>Input for issuing a single-use or bounded-use download link for a stored file.</summary>
/// <param name="FileId">Metadata id of the file the link unlocks. Required.</param>
/// <param name="NotBeforeUtc">Earliest instant the link may be consumed; must not be later than <paramref name="ExpiresAtUtc" />.</param>
/// <param name="ExpiresAtUtc">Instant after which the link is dead regardless of remaining downloads.</param>
/// <param name="WindowStartUtc">Start of an allowed time-of-use window; must not be later than <paramref name="WindowEndUtc" />.</param>
/// <param name="WindowEndUtc">End of the allowed time-of-use window.</param>
/// <param name="MaxDownloads">Maximum successful consumptions; must be greater than zero when supplied. Unlimited when null.</param>
/// <param name="TenantId">Tenant the link belongs to, for multi-tenant stores.</param>
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

/// <summary>A newly issued link. <paramref name="Token" /> is the only time the raw secret is available; the store keeps just its hash.</summary>
/// <param name="LinkId">Identifier of the stored link row, safe to log.</param>
/// <param name="Token">Base64url-encoded secret to hand to the caller. Not recoverable afterwards.</param>
/// <param name="CreatedUtc">When the link was issued.</param>
/// <param name="ExpiresAtUtc">When the link stops working, or null when it has no expiry.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CreateFileDownloadAccessLinkResult(Guid LinkId, string Token, DateTime CreatedUtc, DateTime? ExpiresAtUtc)
{
    /// <inheritdoc />
    public override string ToString() => $"CreateFileDownloadAccessLinkResult: LinkId={LinkId}, CreatedUtc={CreatedUtc:u}, ExpiresAtUtc={ExpiresAtUtc?.ToString("u") ?? "(none)"}";
}

/// <summary>Why a download link was refused. Callers should show one generic message; the specific reason is for audit and diagnostics.</summary>
public enum FileDownloadAccessConsumeFailureReason
{
    /// <summary>No link matched the token, or the file metadata behind it has been deleted.</summary>
    NotFound = 0,

    /// <summary>The link was revoked on purpose.</summary>
    Revoked = 1,

    /// <summary>The link's not-before instant has not arrived.</summary>
    NotYetValid = 2,

    /// <summary>The link's expiry has passed.</summary>
    Expired = 3,

    /// <summary>The current time is outside the link's allowed window.</summary>
    OutsideWindow = 4,

    /// <summary>The link has already been consumed the maximum number of times.</summary>
    MaxDownloadsReached = 5,

    /// <summary>The per-token lock could not be taken, so the consume was not attempted.</summary>
    LockUnavailable = 6,

    /// <summary>The token was empty or not valid base64url.</summary>
    InvalidToken = 7
}

/// <summary>Result of consuming a download link.</summary>
/// <param name="IsAllowed">True when the download may proceed.</param>
/// <param name="FileId">File the link pointed at, when the link was found.</param>
/// <param name="LinkId">Link row that was evaluated, when found.</param>
/// <param name="FailureReason">Why the request was refused; null when allowed.</param>
/// <param name="DownloadCount">Consumption count after this attempt, when known.</param>
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
