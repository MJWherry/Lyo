using Lyo.Exceptions;
using Lyo.Validation.Postgres.Database;
using Lyo.Validation.Postgres.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Validation.Postgres;

/// <summary>PostgreSQL <see cref="IValidationSchemaStore" /> using <see cref="IDbContextFactory{TContext}" />.</summary>
public sealed class PostgresValidationSchemaStore : IValidationSchemaStore
{
    private readonly IDbContextFactory<ValidationDbContext> _contextFactory;

    /// <summary>Creates a store that opens a context per operation.</summary>
    public PostgresValidationSchemaStore(IDbContextFactory<ValidationDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<ValidationSchema?> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Schemas.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct).ConfigureAwait(false);
        return entity == null ? null : ValidationSchemaMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationSchema>> ListAsync(string? targetTypeName = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Schemas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(targetTypeName))
            query = query.Where(s => s.TargetTypeName == targetTypeName);

        var rows = await query.OrderBy(s => s.Key).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ValidationSchemaMapper.ToModel).ToArray();
    }

    /// <inheritdoc />
    public async Task SaveAsync(ValidationSchema schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(schema);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schema.Key);
        ArgumentHelpers.ThrowIfNull(schema.Constraints);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.Schemas.FirstOrDefaultAsync(s => s.Key == schema.Key, ct).ConfigureAwait(false);
        if (existing == null)
            context.Schemas.Add(ValidationSchemaMapper.ToEntity(schema));
        else
            ValidationSchemaMapper.ToEntity(schema, existing);

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var deleted = await context.Schemas.Where(s => s.Key == key).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        return deleted > 0;
    }
}
