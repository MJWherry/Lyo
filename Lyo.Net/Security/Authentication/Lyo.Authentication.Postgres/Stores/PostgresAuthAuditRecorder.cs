using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Postgres.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IAuthAuditRecorder"/>. Persists to <c>[user].[event]</c> via <see cref="UserDbContext"/>. Non-throwing — any persistence exception is caught and
/// (debug) logged because audit failures must never break the authentication codepath.
/// </summary>
public sealed class PostgresAuthAuditRecorder : IAuthAuditRecorder
{
    private readonly IDbContextFactory<UserDbContext> _contextFactory;
    private readonly ILogger<PostgresAuthAuditRecorder> _logger;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    /// <summary>Creates a new recorder.</summary>
    public PostgresAuthAuditRecorder(
        IDbContextFactory<UserDbContext> contextFactory,
        ILogger<PostgresAuthAuditRecorder> logger,
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
    public async Task RecordAsync(AuthAuditEvent evt, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(evt);
        try {
            var resolvedTenant = TenancyResolver.Resolve(evt.TenantId, _featureTenancy, _entityRefOptions);
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            context.Events.Add(new UserEventEntity {
                Id = evt.Id,
                Timestamp = evt.Timestamp,
                Kind = evt.Kind,
                UserId = evt.UserId,
                TenantId = resolvedTenant,
                Subject = evt.Subject,
                Provider = evt.Provider,
                Outcome = evt.Outcome,
                Reason = evt.Reason,
                IpAddress = evt.IpAddress,
                UserAgent = evt.UserAgent,
                CorrelationId = evt.CorrelationId,
                MetadataJson = evt.Metadata is null || evt.Metadata.Count == 0 ? null : JsonSerializer.Serialize(evt.Metadata)
            });

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Failed to persist auth audit event kind={Kind} id={Id}", evt.Kind, evt.Id);
        }
    }
}
