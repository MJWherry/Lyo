using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;

namespace Lyo.Authorization;

/// <summary>Process-local grant store. One lock covers save, revoke, and check.</summary>
public sealed class InMemoryGrantStore : IGrantStore
{
    private readonly object _gate = new();
    private readonly List<Grant> _grants = [];
    private readonly Func<DateTime> _utcNow;

    /// <summary>Builds a store. <paramref name="utcNow" /> defaults to <see cref="DateTime.UtcNow" />.</summary>
    public InMemoryGrantStore(Func<DateTime>? utcNow = null) => _utcNow = utcNow ?? (() => DateTime.UtcNow);

    /// <inheritdoc />
    public string HealthCheckName => "authorization.grants";

    /// <inheritdoc />
    public Task<Grant> SaveAsync(GrantWrite write, CancellationToken ct = default)
    {
        var normalized = GrantRules.Normalize(write);
        var now = Utc();
        lock (_gate) {
            var active = FindActive(normalized);
            if (active is not null && !GrantRules.IsInactive(active, now))
                throw new ConflictException("An active grant already exists for this subject, action, and resource.");

            if (active is not null)
                Replace(active, active with { RevokedAt = now });

            var saved = new Grant {
                Id = LyoGuid.CreateV7(),
                Subject = normalized.Subject,
                Action = normalized.Action,
                ResourceType = normalized.ResourceType,
                ResourceId = normalized.ResourceId,
                TenantId = normalized.TenantId,
                GrantedBy = normalized.GrantedBy,
                ExpiresAt = normalized.ExpiresAt,
                CreatedTimestamp = now
            };

            _grants.Add(saved);
            return Task.FromResult(saved);
        }
    }

    /// <inheritdoc />
    public Task<Grant> RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var now = Utc();
        lock (_gate) {
            var index = _grants.FindIndex(g => g.Id == id);
            if (index < 0)
                throw new NotFoundException("Grant", id);

            var current = _grants[index];
            if (current.RevokedAt is not null)
                return Task.FromResult(current);

            var revoked = current with { RevokedAt = now };
            _grants[index] = revoked;
            return Task.FromResult(revoked);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsAllowedAsync(GrantCheck check, CancellationToken ct = default)
    {
        var normalized = GrantRules.Normalize(check);
        var now = Utc();
        lock (_gate)
            return Task.FromResult(_grants.Any(g => GrantRules.Matches(g, normalized, now)));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Grant>> ListAsync(GrantListQuery? query = null, CancellationToken ct = default)
    {
        var filter = query ?? new GrantListQuery();
        if (filter.Take < 1 || filter.Take > GrantListQuery.MaxTake)
            throw new ArgumentOutOfRangeException(nameof(query), "Take must be from 1 to 1000.");

        var now = Utc();
        lock (_gate) {
            IEnumerable<Grant> rows = _grants;
            var subjectFilter = filter.Subject;
            if (!string.IsNullOrWhiteSpace(subjectFilter)) {
                var subject = subjectFilter.Trim();
                rows = rows.Where(g => g.Subject == subject);
            }

            if (!filter.IncludeRevoked)
                rows = rows.Where(g => g.RevokedAt is null);

            if (!filter.IncludeExpired)
                rows = rows.Where(g => g.ExpiresAt is null || g.ExpiresAt > now);

            var page = rows.OrderByDescending(g => g.CreatedTimestamp).ThenByDescending(g => g.Id).Take(filter.Take).ToArray();
            return Task.FromResult<IReadOnlyList<Grant>>(page);
        }
    }

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero));

    private Grant? FindActive(GrantWrite write)
        => _grants.FirstOrDefault(g
            => g.RevokedAt is null && g.Subject == write.Subject && g.Action == write.Action && g.ResourceType == write.ResourceType && g.ResourceId == write.ResourceId &&
            g.TenantId == write.TenantId);

    private void Replace(Grant current, Grant updated)
    {
        var index = _grants.FindIndex(g => g.Id == current.Id);
        if (index >= 0)
            _grants[index] = updated;
    }

    private DateTime Utc()
    {
        var now = _utcNow();
        if (now.Kind == DateTimeKind.Local)
            throw new ArgumentException("The grant clock returned a local DateTime.");

        return now.Kind == DateTimeKind.Utc ? now : DateTime.SpecifyKind(now, DateTimeKind.Utc);
    }
}