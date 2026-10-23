using Lyo.Audit;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Authorization;

/// <summary>
/// Counts allow and deny, audits saves and revokes, and keeps store failures off the deny counter. <see cref="IMetrics.RecordError" /> is given a fixed message so a provider
/// exception cannot leak a subject.
/// </summary>
public sealed class ResourceAuthorization : IResourceAuthorization
{
    /// <summary>Fixed <see cref="IMetrics.RecordError" /> message.</summary>
    public const string StoreFailedMessage = "authorization.store_failed";

    private readonly IAuditRecorder _audit;
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;

    private readonly IGrantStore _store;

    /// <summary>Wraps <paramref name="store" />. Missing metrics, logger, and audit recorder become no-ops.</summary>
    public ResourceAuthorization(IGrantStore store, IMetrics? metrics = null, ILogger? logger = null, IAuditRecorder? audit = null)
    {
        ArgumentHelpers.ThrowIfNull(store);
        _store = store;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = logger ?? NullLogger.Instance;
        _audit = audit ?? NullAuditRecorder.Instance;
    }

    /// <summary>Saves a grant, audits it, and counts a supersede when the store revoked an expired row first.</summary>
    public async Task<Grant> SaveAsync(GrantWrite write, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(write);
        try {
            var before = await _store.ListAsync(new() { Subject = write.Subject, IncludeExpired = true, Take = GrantListQuery.MaxTake }, ct).ConfigureAwait(false);
            var saved = await _store.SaveAsync(write, ct).ConfigureAwait(false);
            var superseded = before.Any(g
                => g.RevokedAt is null && g.Subject == saved.Subject && g.Action == saved.Action && g.ResourceType == saved.ResourceType && g.ResourceId == saved.ResourceId &&
                g.TenantId == saved.TenantId && g.Id != saved.Id);

            _metrics.IncrementCounter(superseded ? "authorization.grant.superseded" : "authorization.grant.saved");
            await AuditAsync(superseded ? "authorization.grant.superseded" : "authorization.grant.saved", saved.Subject, saved.GrantedBy, ct).ConfigureAwait(false);
            return saved;
        }
        catch (ConflictException) {
            _metrics.IncrementCounter("authorization.grant.conflict");
            throw;
        }
        catch (Exception ex) {
            RecordFailure(ex);
            throw;
        }
    }

    /// <summary>Revokes a grant and audits it. A second revoke still returns the row and is audited again.</summary>
    public async Task<Grant> RevokeAsync(Guid id, CancellationToken ct = default)
    {
        try {
            var revoked = await _store.RevokeAsync(id, ct).ConfigureAwait(false);
            _metrics.IncrementCounter("authorization.grant.revoked");
            await AuditAsync("authorization.grant.revoked", revoked.Subject, revoked.GrantedBy, ct).ConfigureAwait(false);
            return revoked;
        }
        catch (Exception ex) when (ex is not NotFoundException) {
            RecordFailure(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAllowedAsync(GrantCheck check, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(check);
        using var _ = _metrics.StartTimer("authorization.grant.check");
        bool allowed;
        try {
            allowed = await _store.IsAllowedAsync(check, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            RecordFailure(ex);
            throw;
        }

        _metrics.IncrementCounter("authorization.grant.check", tags: [("result", allowed ? "allow" : "deny"), ("action", check.Action), ("resource_type", check.ResourceType)]);
        if (allowed)
            _logger.LogInformation("Grant {Result} {Action} {ResourceType}", "allow", check.Action, check.ResourceType);
        else {
            _logger.LogInformation("Grant {Result} {Action} {ResourceType}", "deny", check.Action, check.ResourceType);
            _logger.LogDebug("Grant deny {Subject} {Action} {ResourceType}", check.Subject, check.Action, check.ResourceType);
        }

        return allowed;
    }

    private async Task AuditAsync(string eventType, string subject, string? grantedBy, CancellationToken ct)
    {
        try {
            EntityRef? actor = null;
            if (!string.IsNullOrWhiteSpace(grantedBy))
                actor = new EntityRef("authorization.actor", grantedBy);

            await _audit.RecordEventAsync(new(new("authorization.subject", subject), eventType, Actor: actor), ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Grant audit failed for {EventType}", eventType);
        }
    }

    private void RecordFailure(Exception ex)
    {
        _metrics.RecordError("authorization.error", new InvalidOperationException(StoreFailedMessage));
        _logger.LogDebug(ex, "Grant store failed");
    }
}