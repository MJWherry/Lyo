using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Services.Users;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Postgres.Stores;

/// <summary>PostgreSQL implementation of <see cref="IUserStore"/>. Persists Lyo users to <c>[user].[user]</c>.</summary>
public sealed class PostgresUserStore : IUserStore
{
    private readonly IDbContextFactory<UserDbContext> _contextFactory;
    private readonly ILogger<PostgresUserStore> _logger;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    /// <summary>Creates a new store.</summary>
    public PostgresUserStore(
        IDbContextFactory<UserDbContext> contextFactory,
        ILogger<PostgresUserStore> logger,
        IOptions<EntityRefOptions> entityRefOptions,
        IOptions<PostgresUserOptions> userOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(logger);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(userOptions);
        _contextFactory = contextFactory;
        _logger = logger;
        _entityRefOptions = entityRefOptions.Value;
        _featureTenancy = userOptions.Value.Tenancy;
    }

    /// <inheritdoc/>
    public async Task<LyoUser?> GetByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Users.AsNoTracking()
            .Where(u => u.TenantId == resolvedTenant)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);

        return entity is null ? null : ToRecord(entity);
    }

    /// <inheritdoc/>
    public async Task<LyoUser?> GetByEmailAsync(string email, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(email);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Users.AsNoTracking()
            .Where(u => u.TenantId == resolvedTenant)
            .FirstOrDefaultAsync(u => u.Email == email, ct)
            .ConfigureAwait(false);

        return entity is null ? null : ToRecord(entity);
    }

    /// <inheritdoc/>
    public async Task<LyoUser> CreateAsync(LyoUser user, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(user);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = ToEntity(user);
        entity.TenantId = resolvedTenant;
        context.Users.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return user;
    }

    /// <inheritdoc/>
    public async Task UpdateLastLoginAsync(Guid id, DateTime utcNow, Guid? tenantId, CancellationToken ct = default)
    {
        try {
            var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            await context.Users
                .Where(u => u.Id == id && u.TenantId == resolvedTenant)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(u => u.LastLoginTimestamp, utcNow)
                        .SetProperty(u => u.UpdatedTimestamp, utcNow),
                    ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "UpdateLastLogin best-effort update failed for user {UserId}", id);
        }
    }

    /// <inheritdoc/>
    public async Task SetScopesAsync(Guid id, IReadOnlyList<string> scopes, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(scopes);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var scopesJson = JsonHelper.SerializeStringList(scopes);
        var now = DateTime.UtcNow;
        var rows = await context.Users
            .Where(u => u.Id == id && u.TenantId == resolvedTenant)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.ScopesJson, scopesJson)
                    .SetProperty(u => u.UpdatedTimestamp, now),
                ct)
            .ConfigureAwait(false);

        if (rows == 0)
            throw new InvalidOperationException($"User id '{id}' not found.");
    }

    /// <inheritdoc/>
    public async Task SetDisabledAsync(Guid id, DateTime? disabledAt, string? reason, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var rows = await context.Users
            .Where(u => u.Id == id && u.TenantId == resolvedTenant)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.DisabledTimestamp, disabledAt)
                    .SetProperty(u => u.DisabledReason, reason)
                    .SetProperty(u => u.UpdatedTimestamp, now),
                ct)
            .ConfigureAwait(false);

        if (rows == 0)
            throw new InvalidOperationException($"User id '{id}' not found.");
    }

    private static UserEntity ToEntity(LyoUser user) =>
        new() {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            AvatarUrl = user.AvatarUrl,
            PreferredLanguageBcp47 = user.PreferredLanguageBcp47,
            ScopesJson = JsonHelper.SerializeStringList(user.Scopes),
            MetadataJson = JsonHelper.SerializeMetadata(user.Metadata),
            PersonId = user.PersonId,
            CreatedTimestamp = user.CreatedAt,
            UpdatedTimestamp = user.UpdatedAt,
            LastLoginTimestamp = user.LastLoginAt,
            DisabledTimestamp = user.DisabledAt,
            DisabledReason = user.DisabledReason
        };

    private static LyoUser ToRecord(UserEntity entity) =>
        new(
            Id: entity.Id,
            DisplayName: entity.DisplayName,
            Email: entity.Email,
            EmailVerified: entity.EmailVerified,
            AvatarUrl: entity.AvatarUrl,
            PreferredLanguageBcp47: entity.PreferredLanguageBcp47,
            Scopes: JsonHelper.DeserializeStringList(entity.ScopesJson),
            Metadata: JsonHelper.DeserializeMetadata(entity.MetadataJson),
            PersonId: entity.PersonId,
            CreatedAt: entity.CreatedTimestamp,
            UpdatedAt: entity.UpdatedTimestamp,
            LastLoginAt: entity.LastLoginTimestamp,
            DisabledAt: entity.DisabledTimestamp,
            DisabledReason: entity.DisabledReason);
}
