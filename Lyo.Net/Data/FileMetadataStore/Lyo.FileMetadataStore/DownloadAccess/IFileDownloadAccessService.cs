namespace Lyo.FileMetadataStore.DownloadAccess;

/// <summary>
/// Issues and redeems bounded-use download links for stored files. Treat the token as a bearer secret: persist only its hash, and serialize concurrent redemptions of the
/// same token so a max-download limit cannot be exceeded by racing callers.
/// </summary>
public interface IFileDownloadAccessService
{
    /// <summary>Issues a link for <paramref name="request" />. The returned token is the only copy of the secret.</summary>
    /// <param name="request">Link constraints. <see cref="CreateFileDownloadAccessLinkRequest.FileId" /> must reference live file metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">The request is internally inconsistent, for example a not-before later than the expiry.</exception>
    /// <exception cref="FileNotFoundException">No live file metadata exists for the requested file id.</exception>
    Task<CreateFileDownloadAccessLinkResult> CreateLinkAsync(CreateFileDownloadAccessLinkRequest request, CancellationToken ct = default);

    /// <summary>
    /// Checks <paramref name="token" /> and, when every constraint holds, atomically records one consumption. Refusals are returned rather than thrown so the caller can audit
    /// them without exception handling; every attempt, allowed or not, is written to the audit trail.
    /// </summary>
    /// <param name="token">Base64url token handed out by <see cref="CreateLinkAsync" />.</param>
    /// <param name="actorId">Caller identity to record in the audit trail.</param>
    /// <param name="ipAddress">Caller address to record in the audit trail.</param>
    /// <param name="nowUtc">Overrides the clock, for tests and for replaying a decision at a known instant.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ConsumeFileDownloadAccessLinkResult> ValidateAndConsumeDownloadAsync(
        string token,
        string? actorId = null,
        string? ipAddress = null,
        DateTime? nowUtc = null,
        CancellationToken ct = default);
}
