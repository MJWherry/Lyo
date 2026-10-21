using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Lyo.Algorithms.Models.Response;
using Lyo.Algorithms.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Algorithms.Postgres;

/// <summary>Postgres <see cref="IAlgorithmDefinitionStore" />. Revision numbers are allocated inside a transaction.</summary>
public sealed class PostgresAlgorithmDefinitionStore : IAlgorithmDefinitionStore, IHealth
{
    private readonly IDbContextFactory<AlgorithmDbContext> _factory;

    /// <summary>Creates the store.</summary>
    public PostgresAlgorithmDefinitionStore(IDbContextFactory<AlgorithmDbContext> factory)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<AlgorithmDefinitionRes> SaveRevisionAsync(string key, AlgorithmRevisionReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        AlgorithmKeys.ThrowIfInvalidKey(key);
        for (var attempt = 0; attempt < 2; attempt++) {
            try {
                return await SaveRevisionCoreAsync(key, request, ct).ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (attempt == 0 && IsUniqueViolation(ex)) { }
        }

        return AlgorithmHelpers.Throw<AlgorithmDefinitionRes>($"Algorithm '{key}' could not allocate a revision.");
    }

    /// <inheritdoc />
    public async Task<AlgorithmDefinitionRes?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Definitions.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct).ConfigureAwait(false);
        return entity is null ? null : ToDefinition(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlgorithmDefinitionRes>> ListAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Definitions.AsNoTracking().OrderBy(x => x.Key).ToListAsync(ct).ConfigureAwait(false);
        return [.. rows.Select(ToDefinition)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlgorithmRevisionRes>> GetRevisionsAsync(string key, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var definition = await ctx.Definitions.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct).ConfigureAwait(false);
        AlgorithmHelpers.ThrowIf(definition is null, $"Algorithm '{key}' was not found.");
        var rows = await ctx.Revisions.AsNoTracking().Where(r => r.DefinitionId == definition.Id).OrderByDescending(r => r.Revision).ToListAsync(ct).ConfigureAwait(false);
        return [.. rows.Select(ToRevision)];
    }

    /// <inheritdoc />
    public async Task<AlgorithmRevisionRes?> GetRevisionAsync(string key, int revision, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var definition = await ctx.Definitions.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct).ConfigureAwait(false);
        if (definition is null)
            return null;

        var row = await ctx.Revisions.AsNoTracking().FirstOrDefaultAsync(r => r.DefinitionId == definition.Id && r.Revision == revision, ct).ConfigureAwait(false);
        return row is null ? null : ToRevision(row);
    }

    /// <inheritdoc />
    public async Task<AlgorithmDefinitionRes> RevertToRevisionAsync(string key, AlgorithmRevertReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var existing = AlgorithmHelpers.ThrowIfNull(
            await GetRevisionAsync(key, request.Revision, ct).ConfigureAwait(false), $"Algorithm '{key}' has no revision {request.Revision}.", "algorithm.revision.missing");

        var definition = await GetByKeyAsync(key, ct).ConfigureAwait(false);
        return await SaveRevisionAsync(
                key, new() {
                    Kind = existing.Kind,
                    Source = existing.Source,
                    Payload = existing.Payload,
                    Name = definition?.Name,
                    Description = definition?.Description
                }, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AlgorithmLabelRes> SetLabelAsync(string key, string label, AlgorithmLabelReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        AlgorithmKeys.ThrowIfInvalidLabel(label);
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var definition = AlgorithmHelpers.ThrowIfNull(
            await ctx.Definitions.FirstOrDefaultAsync(x => x.Key == key, ct).ConfigureAwait(false), $"Algorithm '{key}' was not found.", "algorithm.definition.missing");

        var exists = await ctx.Revisions.AnyAsync(r => r.DefinitionId == definition.Id && r.Revision == request.Revision, ct).ConfigureAwait(false);
        AlgorithmHelpers.ThrowIf(!exists, $"Algorithm '{key}' has no revision {request.Revision}.", "algorithm.revision.missing");
        var row = await ctx.Labels.FirstOrDefaultAsync(l => l.DefinitionId == definition.Id && l.Label == label, ct).ConfigureAwait(false);
        if (row is null) {
            row = new() {
                DefinitionId = definition.Id,
                Label = label,
                Revision = request.Revision,
                IsMutable = request.IsMutable
            };

            ctx.Labels.Add(row);
        }
        else {
            AlgorithmHelpers.ThrowIf(row.IsMutable != request.IsMutable, $"Label '{label}' on '{key}' mutability cannot change.", "algorithm.label.mutability");
            AlgorithmHelpers.ThrowIf(!row.IsMutable && row.Revision != request.Revision, $"Label '{label}' on '{key}' is immutable.", "algorithm.label.immutable");
            row.Revision = request.Revision;
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToLabel(row);
    }

    /// <inheritdoc />
    public async Task<AlgorithmLabelRes?> GetLabelAsync(string key, string label, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var definition = await ctx.Definitions.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct).ConfigureAwait(false);
        if (definition is null)
            return null;

        var row = await ctx.Labels.AsNoTracking().FirstOrDefaultAsync(l => l.DefinitionId == definition.Id && l.Label == label, ct).ConfigureAwait(false);
        return row is null ? null : ToLabel(row);
    }

    /// <inheritdoc />
    public string HealthCheckName => "algorithms-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_factory, PostgresAlgorithmOptions.Schema, ct);

    private async Task<AlgorithmDefinitionRes> SaveRevisionCoreAsync(string key, AlgorithmRevisionReq request, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Definitions.FromSqlRaw($"SELECT * FROM {PostgresAlgorithmOptions.Schema}.algorithm_definition WHERE key = {{0}} FOR UPDATE", key)
            .AsTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (entity is null) {
            entity = new() { Id = LyoGuid.CreateV7(), Key = key, CreatedTimestamp = DateTime.UtcNow };
            ctx.Definitions.Add(entity);
        }

        var max = await ctx.Revisions.Where(r => r.DefinitionId == entity.Id).Select(r => (int?)r.Revision).MaxAsync(ct).ConfigureAwait(false) ?? 0;
        var now = DateTime.UtcNow;
        ctx.Revisions.Add(
            new() {
                DefinitionId = entity.Id,
                Revision = max + 1,
                Kind = request.Kind.ToString(),
                Source = request.Source,
                Payload = request.Payload,
                CreatedTimestamp = now
            });

        entity.Kind = request.Kind.ToString();
        entity.CurrentRevision = max + 1;
        entity.UpdatedTimestamp = now;
        if (request.Name is not null)
            entity.Name = request.Name;

        if (request.Description is not null)
            entity.Description = request.Description;

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
        return ToDefinition(entity);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("23505", StringComparison.Ordinal) || text.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
    }

    private static AlgorithmDefinitionRes ToDefinition(AlgorithmDefinitionEntity entity)
        => new() {
            Id = entity.Id,
            Key = entity.Key,
            Name = entity.Name,
            Description = entity.Description,
            Kind = Enum.Parse<AlgorithmKind>(entity.Kind),
            CurrentRevision = entity.CurrentRevision,
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp
        };

    private static AlgorithmRevisionRes ToRevision(AlgorithmRevisionEntity entity)
        => new() {
            DefinitionId = entity.DefinitionId,
            Revision = entity.Revision,
            Kind = Enum.Parse<AlgorithmKind>(entity.Kind),
            Source = entity.Source,
            Payload = entity.Payload,
            CreatedTimestamp = entity.CreatedTimestamp
        };

    private static AlgorithmLabelRes ToLabel(AlgorithmLabelEntity entity)
        => new() {
            DefinitionId = entity.DefinitionId,
            Label = entity.Label,
            Revision = entity.Revision,
            IsMutable = entity.IsMutable
        };
}