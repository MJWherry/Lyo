using Lyo.Diagram.Models;
using Lyo.Diagram.Postgres.Database;
using Lyo.Diagram.Postgres.Mapping;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Diagram.Postgres;

/// <summary>PostgreSQL <see cref="IDiagramStore" /> that opens a context via <see cref="IDbContextFactory{TContext}" />.</summary>
public sealed class PostgresDiagramStore : IDiagramStore
{
    private readonly IDbContextFactory<DiagramDbContext> _contextFactory;

    /// <summary>Mints a store that opens a context per operation.</summary>
    public PostgresDiagramStore(IDbContextFactory<DiagramDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<DiagramDocument?> GetAsync(Guid id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfEmpty(id);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Diagrams.AsNoTracking().FirstOrDefaultAsync(diagram => diagram.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : DiagramMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DiagramSummary>> ListAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Diagrams.AsNoTracking()
            .OrderBy(diagram => diagram.Name)
            .ThenBy(diagram => diagram.Id)
            .Select(diagram => new DiagramSummary { Id = diagram.Id, Name = diagram.Name, UpdatedTimestamp = diagram.UpdatedTimestamp ?? diagram.CreatedTimestamp })
            .ToArrayAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(DiagramDocument diagram, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(diagram);
        ArgumentHelpers.ThrowIfEmpty(diagram.Id);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(diagram.Name);
        ArgumentHelpers.ThrowIf(diagram.Name.Length > 200, "Diagram name cannot exceed 200 characters.", nameof(diagram));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.Diagrams.FirstOrDefaultAsync(row => row.Id == diagram.Id, ct).ConfigureAwait(false);
        if (existing == null)
            context.Diagrams.Add(DiagramMapper.ToEntity(diagram));
        else
            DiagramMapper.ToEntity(diagram, existing);

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfEmpty(id);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var deleted = await context.Diagrams.Where(diagram => diagram.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        return deleted > 0;
    }
}