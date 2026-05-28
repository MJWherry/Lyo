using System.Diagnostics;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Services.Opaque;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Postgres.Stores;

/// <summary>PostgreSQL implementation of <see cref="IApiTokenStore" />. Persists Format-B tokens to <c>[user].[token]</c>.</summary>
public sealed class PostgresApiTokenStore : IApiTokenStore, IHealth
{
    private readonly IDbContextFactory<UserDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;
    private readonly ILogger<PostgresApiTokenStore> _logger;

    /// <summary>Creates a new store.</summary>
    public PostgresApiTokenStore(
        IDbContextFactory<UserDbContext> contextFactory,
        ILogger<PostgresApiTokenStore> logger,
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

    /// <inheritdoc />
    public async Task InsertAsync(ApiTokenRecord record, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(record);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = ToEntity(record);
        entity.TenantId = resolvedTenant;
        context.Tokens.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiTokenRecord?> GetByIdAsync(string id, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Tokens.AsNoTracking().Where(t => t.TenantId == resolvedTenant).FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        return entity is null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task TouchLastUsedAsync(string id, DateTime utcNow, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        try {
            var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var rows = await context.Tokens.Where(t => t.Id == id && t.TenantId == resolvedTenant)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.LastUsedTimestamp, utcNow).SetProperty(t => t.UpdatedTimestamp, utcNow), ct)
                .ConfigureAwait(false);

            if (rows == 0)
                _logger.LogDebug("TouchLastUsed: token {TokenId} not found (likely deleted concurrently)", id);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "TouchLastUsed best-effort update failed for token {TokenId}", id);
        }
    }

    /// <inheritdoc />
    public async Task RevokeAsync(string id, DateTime revokedAt, string? reason, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Tokens.Where(t => t.Id == id && t.TenantId == resolvedTenant)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedTimestamp, revokedAt).SetProperty(t => t.RevokedReason, reason).SetProperty(t => t.UpdatedTimestamp, revokedAt), ct)
            .ConfigureAwait(false);

        if (rows == 0)
            throw new InvalidOperationException($"Token id '{id}' not found.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiTokenRecord>> ListForUserAsync(Guid userId, bool includeRevoked, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tokens.AsNoTracking().Where(t => t.UserId == userId && t.TenantId == resolvedTenant);
        if (!includeRevoked)
            query = query.Where(t => t.RevokedTimestamp == null);

        var rows = await query.OrderBy(t => t.CreatedTimestamp).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToRecord).ToArray();
    }

    /// <inheritdoc />
    public string HealthCheckName => "lyo-authentication-token-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "user" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private static TokenEntity ToEntity(ApiTokenRecord record)
        => new() {
            Id = record.Id,
            SecretHash = record.SecretHash,
            Kind = record.Kind,
            Ring = record.Ring,
            UserId = record.UserId,
            DisplayName = record.DisplayName,
            ScopesJson = JsonHelper.SerializeStringList(record.Scopes),
            MetadataJson = JsonHelper.SerializeMetadata(record.Metadata),
            CreatedTimestamp = record.CreatedAt,
            UpdatedTimestamp = record.UpdatedAt,
            ExpiresTimestamp = record.ExpiresAt,
            LastUsedTimestamp = record.LastUsedAt,
            RevokedTimestamp = record.RevokedAt,
            RevokedReason = record.RevokedReason,
            RotatedFromId = record.RotatedFromId
        };

    private static ApiTokenRecord ToRecord(TokenEntity entity)
        => new(
            entity.Id, entity.SecretHash, entity.Kind, entity.Ring, entity.UserId, entity.DisplayName, JsonHelper.DeserializeStringList(entity.ScopesJson),
            JsonHelper.DeserializeMetadata(entity.MetadataJson), entity.CreatedTimestamp, entity.UpdatedTimestamp, entity.ExpiresTimestamp, entity.LastUsedTimestamp,
            entity.RevokedTimestamp, entity.RevokedReason, entity.RotatedFromId);
}