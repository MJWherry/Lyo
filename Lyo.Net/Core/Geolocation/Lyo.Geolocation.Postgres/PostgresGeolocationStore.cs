using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Postgres.Database;
using Lyo.Geolocation.Postgres.Mapping;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Geolocation.Postgres;

/// <summary>PostgreSQL implementation of <see cref="IGeolocationStore" />.</summary>
public sealed class PostgresGeolocationStore : IGeolocationStore, IHealth
{
    private readonly IDbContextFactory<GeolocationDbContext> _contextFactory;

    public PostgresGeolocationStore(IDbContextFactory<GeolocationDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<Address?> GetAddressByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : AddressEntityMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<Address?> GetBySourceAsync(EntityRef source, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Addresses.AsNoTracking()
            .FirstOrDefaultAsync(a => a.SourceEntityType == source.EntityType && a.SourceEntityId == source.EntityId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : AddressEntityMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task SaveAddressAsync(Address address, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(address);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var mapped = AddressEntityMapper.ToEntity(address);
        AddressEntity entity;
        if (address.Id != default) {
            entity = await context.Addresses.FirstOrDefaultAsync(a => a.Id == address.Id, ct).ConfigureAwait(false) ?? mapped;
            if (entity.Id == mapped.Id) {
                context.Entry(entity).CurrentValues.SetValues(mapped);
                if (mapped.Coordinates != null)
                    entity.Coordinates = mapped.Coordinates;
            }
            else {
                context.Addresses.Add(mapped);
                entity = mapped;
            }
        }
        else {
            context.Addresses.Add(mapped);
            entity = mapped;
        }

        address.Id = entity.Id;
        EntitySourceMapping.ApplySource(entity, address.Source);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "geolocation-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = PostgresGeolocationOptions.Schema })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }
}
