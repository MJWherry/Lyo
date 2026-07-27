using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Services.Users;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Postgres.Stores;

/// <summary>PostgreSQL implementation of <see cref="IExternalIdentityStore" />. Persists OIDC identity links to <c>[user].[linked_identity]</c>.</summary>
public sealed class PostgresExternalIdentityStore : IExternalIdentityStore
{
    private readonly IDbContextFactory<UserDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    /// <summary>Creates a new store.</summary>
    public PostgresExternalIdentityStore(IDbContextFactory<UserDbContext> contextFactory, IOptions<EntityRefOptions> entityRefOptions, IOptions<PostgresUserOptions> userOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(userOptions);
        _contextFactory = contextFactory;
        _entityRefOptions = entityRefOptions.Value;
        _featureTenancy = userOptions.Value.Tenancy;
    }

    /// <inheritdoc />
    public async Task<LinkedIdentity?> FindByProviderSubjectAsync(string provider, string subject, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.LinkedIdentities.AsNoTracking()
            .FirstOrDefaultAsync(l => l.TenantId == resolvedTenant && l.Provider == provider && l.Subject == subject && l.UnlinkedTimestamp == null, ct)
            .ConfigureAwait(false);

        return entity is null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<LinkedIdentity> LinkAsync(
        Guid userId,
        string provider,
        string subject,
        string? emailAtLink,
        IReadOnlyList<string> scopes,
        IReadOnlyDictionary<string, object?>? rawClaims,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject);
        ArgumentHelpers.ThrowIfNull(scopes);
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.LinkedIdentities.FirstOrDefaultAsync(
                l => l.TenantId == resolvedTenant && l.Provider == provider && l.Subject == subject && l.UnlinkedTimestamp == null, ct)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var scopesJson = JsonHelper.SerializeStringList(scopes);
        var rawClaimsJson = JsonHelper.SerializeMetadata(rawClaims);
        if (existing is not null) {
            if (existing.UserId != userId)
                throw new ConflictException($"({provider}, {subject}) is already linked to a different Lyo user.");

            existing.EmailAtLink = emailAtLink ?? existing.EmailAtLink;
            existing.ScopesJson = scopesJson;
            existing.RawClaimsJson = rawClaimsJson;
            existing.UpdatedTimestamp = now;
            existing.LastUsedTimestamp = now;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            return ToRecord(existing);
        }

        var entity = new LinkedIdentityEntity {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = resolvedTenant,
            Provider = provider,
            Subject = subject,
            EmailAtLink = emailAtLink,
            ScopesJson = scopesJson,
            RawClaimsJson = rawClaimsJson,
            LinkedTimestamp = now,
            UpdatedTimestamp = now,
            LastUsedTimestamp = now,
            UnlinkedTimestamp = null
        };

        context.LinkedIdentities.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinkedIdentity>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.LinkedIdentities.AsNoTracking()
            .Where(l => l.UserId == userId && l.TenantId == resolvedTenant && l.UnlinkedTimestamp == null)
            .OrderBy(l => l.LinkedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(ToRecord).ToArray();
    }

    /// <inheritdoc />
    public async Task UnlinkAsync(Guid linkedIdentityId, DateTime utcNow, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.LinkedIdentities.Where(l => l.Id == linkedIdentityId && l.TenantId == resolvedTenant)
            .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.UnlinkedTimestamp, utcNow).SetProperty(l => l.UpdatedTimestamp, utcNow), ct)
            .ConfigureAwait(false);

        if (rows == 0)
            throw new NotFoundException($"Linked identity '{linkedIdentityId}' not found.");
    }

    private static LinkedIdentity ToRecord(LinkedIdentityEntity entity)
        => new(
            entity.Id, entity.UserId, entity.Provider, entity.Subject, entity.EmailAtLink, JsonHelper.DeserializeStringList(entity.ScopesJson),
            JsonHelper.DeserializeMetadata(entity.RawClaimsJson), entity.LinkedTimestamp, entity.UpdatedTimestamp, entity.LastUsedTimestamp, entity.UnlinkedTimestamp);
}