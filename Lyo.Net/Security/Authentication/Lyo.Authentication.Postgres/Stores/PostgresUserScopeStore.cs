using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Services.Users;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Postgres.Stores;

/// <summary>PostgreSQL <see cref="IUserScopeStore" />. Persists authorization scopes to <c>[user].[scope]</c>.</summary>
public sealed class PostgresUserScopeStore : IUserScopeStore
{
    private readonly IDbContextFactory<UserDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;
    private readonly ILogger<PostgresUserScopeStore> _logger;

    /// <summary>Builds a new store.</summary>
    public PostgresUserScopeStore(
        IDbContextFactory<UserDbContext> contextFactory,
        ILogger<PostgresUserScopeStore> logger,
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
    public async Task<IReadOnlyList<LyoUserScope>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Scopes.AsNoTracking()
            .Where(s => s.UserId == userId && s.TenantId == resolvedTenant)
            .OrderBy(s => s.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(ToRecord).ToArray();
    }

    /// <inheritdoc />
    public async Task<LyoUserScope> CreateAsync(LyoUserScope scope, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(scope);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var duplicate = await context.Scopes.AsNoTracking()
            .AnyAsync(s => s.UserId == scope.UserId && s.Name == scope.Name && s.TenantId == resolvedTenant, ct)
            .ConfigureAwait(false);
        if (duplicate)
            throw new ConflictException($"Scope '{scope.Name}' is already granted to user '{scope.UserId}'.");

        var entity = ToEntity(scope);
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        entity.TenantId = resolvedTenant;
        if (entity.CreatedTimestamp == default)
            entity.CreatedTimestamp = DateTime.UtcNow;

        context.Scopes.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Created scope {ScopeId} name {Name} for user {UserId}", entity.Id, entity.Name, entity.UserId);
        return ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Scopes.Where(s => s.Id == id && s.TenantId == resolvedTenant).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        if (rows == 0)
            throw new NotFoundException($"Scope id '{id}' not found.");
    }

    private static UserScopeEntity ToEntity(LyoUserScope scope)
        => new() {
            Id = scope.Id,
            UserId = scope.UserId,
            Name = scope.Name,
            CreatedTimestamp = scope.CreatedAt,
            UpdatedTimestamp = scope.UpdatedAt
        };

    private static LyoUserScope ToRecord(UserScopeEntity entity)
        => new(entity.Id, entity.UserId, entity.Name, entity.CreatedTimestamp, entity.UpdatedTimestamp);
}
