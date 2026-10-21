using Lyo.Authorization.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Authorization.Postgres;

/// <summary>Postgres grant store. Each call opens a context from <see cref="IDbContextFactory{AuthorizationDbContext}" />. The handler must not capture a context.</summary>
public sealed class PostgresGrantStore : IGrantStore
{
    private readonly IDbContextFactory<AuthorizationDbContext> _factory;
    private readonly Func<DateTime> _utcNow;

    /// <summary>Builds a store.</summary>
    public PostgresGrantStore(IDbContextFactory<AuthorizationDbContext> factory, Func<DateTime>? utcNow = null)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        _factory = factory;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <inheritdoc />
    public string HealthCheckName => "authorization.grants";

    /// <inheritdoc />
    public async Task<Grant> SaveAsync(GrantWrite write, CancellationToken ct = default)
    {
        var normalized = GrantRules.Normalize(write);
        var now = Utc();
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var active = await db.Grants.FirstOrDefaultAsync(
                g => g.RevokedAt == null && g.Subject == normalized.Subject && g.Action == normalized.Action && g.ResourceType == normalized.ResourceType &&
                    g.ResourceId == normalized.ResourceId && g.TenantId == normalized.TenantId, ct)
            .ConfigureAwait(false);

        if (active is not null) {
            var inForce = active.ExpiresAt is null || active.ExpiresAt > now;
            if (inForce)
                throw new ConflictException("An active grant already exists for this subject, action, and resource.");

            active.RevokedAt = now;
        }

        var entity = new GrantEntity {
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

        db.Grants.Add(entity);
        try {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505") {
            throw new ConflictException("An active grant already exists for this subject, action, and resource.");
        }

        return ToGrant(entity);
    }

    /// <inheritdoc />
    public async Task<Grant> RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var now = Utc();
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Grants.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        if (entity is null)
            throw new NotFoundException("Grant", id);

        if (entity.RevokedAt is null) {
            entity.RevokedAt = now;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return ToGrant(entity);
    }

    /// <inheritdoc />
    public async Task<bool> IsAllowedAsync(GrantCheck check, CancellationToken ct = default)
    {
        var normalized = GrantRules.Normalize(check);
        var now = Utc();
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = db.Grants.Where(g
            => g.Subject == normalized.Subject && g.Action == normalized.Action && g.ResourceType == normalized.ResourceType && g.RevokedAt == null &&
            (g.ExpiresAt == null || g.ExpiresAt > now));

        query = normalized.ResourceId is null ? query.Where(g => g.ResourceId == null) : query.Where(g => g.ResourceId == null || g.ResourceId == normalized.ResourceId);
        query = normalized.TenantId is null ? query.Where(g => g.TenantId == null) : query.Where(g => g.TenantId == null || g.TenantId == normalized.TenantId);
        return await query.AnyAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Grant>> ListAsync(GrantListQuery? query = null, CancellationToken ct = default)
    {
        var filter = query ?? new GrantListQuery();
        if (filter.Take < 1 || filter.Take > GrantListQuery.MaxTake)
            throw new ArgumentOutOfRangeException(nameof(query));

        var now = Utc();
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = db.Grants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Subject)) {
            var subject = filter.Subject.Trim();
            rows = rows.Where(g => g.Subject == subject);
        }

        if (!filter.IncludeRevoked)
            rows = rows.Where(g => g.RevokedAt == null);

        if (!filter.IncludeExpired)
            rows = rows.Where(g => g.ExpiresAt == null || g.ExpiresAt > now);

        var page = await rows.OrderByDescending(g => g.CreatedTimestamp).ThenByDescending(g => g.Id).Take(filter.Take).ToListAsync(ct).ConfigureAwait(false);
        return page.Select(ToGrant).ToArray();
    }

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var started = DateTime.UtcNow;
        try {
            await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var ok = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);
            return ok ? HealthResult.Healthy(DateTime.UtcNow - started) : HealthResult.Unhealthy(DateTime.UtcNow - started, "Authorization store could not connect.");
        }
        catch (Exception ex) {
            return HealthResult.Unhealthy(DateTime.UtcNow - started, "Authorization store probe failed.", exception: ex);
        }
    }

    private static Grant ToGrant(GrantEntity entity)
        => new() {
            Id = entity.Id,
            Subject = entity.Subject,
            Action = entity.Action,
            ResourceType = entity.ResourceType,
            ResourceId = entity.ResourceId,
            TenantId = entity.TenantId,
            GrantedBy = entity.GrantedBy,
            ExpiresAt = entity.ExpiresAt,
            RevokedAt = entity.RevokedAt,
            CreatedTimestamp = entity.CreatedTimestamp
        };

    private DateTime Utc()
    {
        var now = _utcNow();
        if (now.Kind == DateTimeKind.Local)
            throw new ArgumentException("The grant clock returned a local DateTime.");

        return now.Kind == DateTimeKind.Utc ? now : DateTime.SpecifyKind(now, DateTimeKind.Utc);
    }
}