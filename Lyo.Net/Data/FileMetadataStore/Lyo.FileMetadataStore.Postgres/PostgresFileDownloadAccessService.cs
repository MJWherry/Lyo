using System.Security.Cryptography;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Audit;
using Lyo.Lock.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileMetadataStore.Postgres;

public sealed class PostgresFileDownloadAccessService : IFileDownloadAccessService
{
    private static readonly TimeSpan DefaultLockAcquireTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DefaultLockDuration = TimeSpan.FromSeconds(10);
    private readonly FileMetadataStoreDbContext _dbContext;
    private readonly ILogger<PostgresFileDownloadAccessService> _logger;
    private readonly ILockService _lockService;

    public PostgresFileDownloadAccessService(FileMetadataStoreDbContext dbContext, ILockService lockService, ILoggerFactory? loggerFactory = null)
    {
        ArgumentHelpers.ThrowIfNull(dbContext);
        ArgumentHelpers.ThrowIfNull(lockService);
        _dbContext = dbContext;
        _lockService = lockService;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<PostgresFileDownloadAccessService>();
    }

    public async Task<CreateFileDownloadAccessLinkResult> CreateLinkAsync(CreateFileDownloadAccessLinkRequest request, CancellationToken ct = default)
    {
        if (request.FileId == Guid.Empty)
            throw new ArgumentException("FileId is required.", nameof(request));
        if (request.MaxDownloads is <= 0)
            throw new ArgumentException("MaxDownloads must be > 0 when provided.", nameof(request));
        if (request.WindowStartUtc.HasValue && request.WindowEndUtc.HasValue && request.WindowStartUtc > request.WindowEndUtc)
            throw new ArgumentException("WindowStartUtc must be <= WindowEndUtc.", nameof(request));
        if (request.NotBeforeUtc.HasValue && request.ExpiresAtUtc.HasValue && request.NotBeforeUtc > request.ExpiresAtUtc)
            throw new ArgumentException("NotBeforeUtc must be <= ExpiresAtUtc.", nameof(request));

        var fileExists = await _dbContext.FileMetadata.AsNoTracking().AnyAsync(i => i.Id == request.FileId.ToString(), ct).ConfigureAwait(false);
        if (!fileExists)
            throw new FileNotFoundException($"Cannot create access link because file metadata does not exist for fileId '{request.FileId}'.");

        var rawToken = RandomNumberGenerator.GetBytes(32);
        var token = Base64UrlEncode(rawToken);
        var tokenHash = SHA256.HashData(rawToken);
        var now = DateTime.UtcNow;
        var link = new FileDownloadAccessLinkEntity {
            Id = Guid.NewGuid(),
            FileId = request.FileId,
            TokenHash = tokenHash,
            CreatedUtc = now,
            NotBeforeUtc = request.NotBeforeUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            WindowStartUtc = request.WindowStartUtc,
            WindowEndUtc = request.WindowEndUtc,
            MaxDownloads = request.MaxDownloads,
            TenantId = request.TenantId
        };

        _dbContext.FileDownloadAccessLinks.Add(link);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return new(link.Id, token, link.CreatedUtc, link.ExpiresAtUtc);
    }

    public async Task<ConsumeFileDownloadAccessLinkResult> ValidateAndConsumeDownloadAsync(
        string token,
        string? actorId = null,
        string? ipAddress = null,
        DateTime? nowUtc = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return await FailAsync(FileDownloadAccessConsumeFailureReason.InvalidToken, null, actorId, ipAddress, "token_empty", ct).ConfigureAwait(false);

        byte[] tokenBytes;
        try {
            tokenBytes = Base64UrlDecode(token);
        }
        catch {
            return await FailAsync(FileDownloadAccessConsumeFailureReason.InvalidToken, null, actorId, ipAddress, "token_invalid_format", ct).ConfigureAwait(false);
        }

        var tokenHash = SHA256.HashData(tokenBytes);
        var lockKey = "appstore:download-link:" + Convert.ToHexString(tokenHash).ToLowerInvariant();
        await using var handle = await _lockService.AcquireAsync(lockKey, DefaultLockAcquireTimeout, DefaultLockDuration, ct).ConfigureAwait(false);
        if (handle == null)
            return await FailAsync(FileDownloadAccessConsumeFailureReason.LockUnavailable, null, actorId, ipAddress, "lock_unavailable", ct).ConfigureAwait(false);

        var now = nowUtc ?? DateTime.UtcNow;
        var link = await _dbContext.FileDownloadAccessLinks.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct).ConfigureAwait(false);
        if (link == null)
            return await FailAsync(FileDownloadAccessConsumeFailureReason.NotFound, null, actorId, ipAddress, "not_found", ct).ConfigureAwait(false);

        if (link.IsRevoked)
            return await FailAsync(FileDownloadAccessConsumeFailureReason.Revoked, link, actorId, ipAddress, "revoked", ct).ConfigureAwait(false);
        if (link.NotBeforeUtc.HasValue && now < link.NotBeforeUtc.Value)
            return await FailAsync(FileDownloadAccessConsumeFailureReason.NotYetValid, link, actorId, ipAddress, "not_before", ct).ConfigureAwait(false);
        if (link.ExpiresAtUtc.HasValue && now > link.ExpiresAtUtc.Value)
            return await FailAsync(FileDownloadAccessConsumeFailureReason.Expired, link, actorId, ipAddress, "expired", ct).ConfigureAwait(false);
        if ((link.WindowStartUtc.HasValue && now < link.WindowStartUtc.Value) || (link.WindowEndUtc.HasValue && now > link.WindowEndUtc.Value))
            return await FailAsync(FileDownloadAccessConsumeFailureReason.OutsideWindow, link, actorId, ipAddress, "outside_window", ct).ConfigureAwait(false);

        var updateCount = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE filestore.file_download_access_links
                 SET download_count = download_count + 1,
                     last_consumed_utc = {now}
                 WHERE id = {link.Id}
                   AND is_revoked = FALSE
                   AND (max_downloads IS NULL OR download_count < max_downloads)
                 """, ct)
            .ConfigureAwait(false);

        if (updateCount <= 0) {
            var latest = await _dbContext.FileDownloadAccessLinks.AsNoTracking().FirstOrDefaultAsync(i => i.Id == link.Id, ct).ConfigureAwait(false);
            var failureReason = latest is { IsRevoked: true } ? FileDownloadAccessConsumeFailureReason.Revoked : FileDownloadAccessConsumeFailureReason.MaxDownloadsReached;
            return await FailAsync(failureReason, latest, actorId, ipAddress, "count_exhausted", ct).ConfigureAwait(false);
        }

        var updatedDownloadCount = (link.DownloadCount + 1);
        await AppendAuditAsync(FileAuditEventType.AccessLinkAllowed.ToString(), "Success", link.FileId, actorId, ipAddress, null, ct).ConfigureAwait(false);
        _logger.LogDebug("Access link {AccessLinkId} consumed for file {FileId}; downloadCount={DownloadCount}", link.Id, link.FileId, updatedDownloadCount);
        return new(true, link.FileId, link.Id, null, updatedDownloadCount);
    }

    private async Task<ConsumeFileDownloadAccessLinkResult> FailAsync(
        FileDownloadAccessConsumeFailureReason reason,
        FileDownloadAccessLinkEntity? link,
        string? actorId,
        string? ipAddress,
        string error,
        CancellationToken ct)
    {
        await AppendAuditAsync(FileAuditEventType.AccessLinkDenied.ToString(), "Failure", link?.FileId, actorId, ipAddress, error, ct).ConfigureAwait(false);
        return new(false, link?.FileId, link?.Id, reason, link?.DownloadCount);
    }

    private async Task AppendAuditAsync(string eventType, string outcome, Guid? fileId, string? actorId, string? ipAddress, string? error, CancellationToken ct)
    {
        var normalizedError = string.IsNullOrWhiteSpace(ipAddress) ? error : $"{error ?? "n/a"};ip={ipAddress}";
        _dbContext.FileAuditEvents.Add(
            new() {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Outcome = outcome,
                Timestamp = DateTime.UtcNow,
                FileId = fileId,
                ActorId = actorId,
                Error = normalizedError
            });
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> input)
        => Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };
        return Convert.FromBase64String(padded);
    }
}
