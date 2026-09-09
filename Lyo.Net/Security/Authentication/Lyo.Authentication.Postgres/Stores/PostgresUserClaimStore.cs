using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Services.Users;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Postgres.Stores;

/// <summary>PostgreSQL <see cref="IUserClaimStore" />. Persists extra JWT claims to <c>[user].[claim]</c>.</summary>
public sealed class PostgresUserClaimStore : IUserClaimStore
{
    private readonly IDbContextFactory<UserDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;
    private readonly ILogger<PostgresUserClaimStore> _logger;

    /// <summary>Builds a new store.</summary>
    public PostgresUserClaimStore(
        IDbContextFactory<UserDbContext> contextFactory,
        ILogger<PostgresUserClaimStore> logger,
        EntityRefOptions entityRefOptions,
        PostgresUserOptions userOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(logger);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(userOptions);
        _contextFactory = contextFactory;
        _logger = logger;
        _entityRefOptions = entityRefOptions;
        _featureTenancy = userOptions.Tenancy;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LyoUserClaim>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Claims.AsNoTracking()
            .Where(c => c.UserId == userId && c.TenantId == resolvedTenant)
            .OrderBy(c => c.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(ToRecord).ToArray();
    }

    /// <inheritdoc />
    public async Task<LyoUserClaim> CreateAsync(LyoUserClaim claim, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(claim);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = ToEntity(claim);
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        entity.TenantId = resolvedTenant;
        if (entity.CreatedTimestamp == default)
            entity.CreatedTimestamp = DateTime.UtcNow;

        context.Claims.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Created claim {ClaimId} type {Type} for user {UserId}", entity.Id, entity.Type, entity.UserId);
        return ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Claims.Where(c => c.Id == id && c.TenantId == resolvedTenant).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        if (rows == 0)
            throw new NotFoundException($"Claim id '{id}' not found.");
    }

    private static UserClaimEntity ToEntity(LyoUserClaim claim)
        => new() {
            Id = claim.Id,
            UserId = claim.UserId,
            Type = claim.Type,
            Value = claim.Value,
            CreatedTimestamp = claim.CreatedAt,
            UpdatedTimestamp = claim.UpdatedAt
        };

    private static LyoUserClaim ToRecord(UserClaimEntity entity)
        => new(entity.Id, entity.UserId, entity.Type, entity.Value, entity.CreatedTimestamp, entity.UpdatedTimestamp);
}
