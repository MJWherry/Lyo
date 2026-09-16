using Lyo.Exceptions;
using Lyo.StateMachine.Models;
using Lyo.StateMachine.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.StateMachine.Postgres;

/// <summary>PostgreSQL <see cref="IStateMachineDefinitionStore" />. Each revision is an immutable <c>jsonb</c> row keyed by (name, revision).</summary>
/// <remarks>Two writers publishing the same next revision race on the primary key; the loser gets false and should reload and retry.</remarks>
public sealed class PostgresStateMachineDefinitionStore : IStateMachineDefinitionStore
{
    private readonly IDbContextFactory<StateMachineDbContext> _contextFactory;

    /// <summary>Creates a store that opens a context per operation.</summary>
    public PostgresStateMachineDefinitionStore(IDbContextFactory<StateMachineDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync(StateMachineDocument document, int expectedRevision, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(document);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(document.Name);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var latest = await context.Definitions.Where(d => d.Name == document.Name).MaxAsync(d => (int?)d.Revision, ct).ConfigureAwait(false) ?? 0;
        if (latest != expectedRevision)
            return false;

        var revision = expectedRevision + 1;
        context.Definitions.Add(new() {
            Name = document.Name,
            Revision = revision,
            Document = StateMachineDocumentJson.Serialize(document with { Revision = revision }),
            CreatedTimestamp = DateTime.UtcNow
        });

        try {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<StateMachineDocument?> GetAsync(string name, int? revision = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Definitions.AsNoTracking().Where(d => d.Name == name);
        query = revision is { } pinned ? query.Where(d => d.Revision == pinned) : query.OrderByDescending(d => d.Revision);
        var row = await query.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return row is null ? null : StateMachineDocumentJson.Deserialize(row.Document) with { Revision = row.Revision };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Definitions.Select(d => d.Name).Distinct().OrderBy(n => n).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> ListRevisionsAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Definitions.Where(d => d.Name == name).OrderBy(d => d.Revision).Select(d => d.Revision).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Definitions.Where(d => d.Name == name).ExecuteDeleteAsync(ct).ConfigureAwait(false) > 0;
    }
}
