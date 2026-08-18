using System.Collections.Concurrent;
using Lyo.Exceptions;

namespace Lyo.Validation;

/// <summary>Thread-safe in-memory <see cref="IValidationSchemaStore" /> for tests and hosts that seed schemas at startup.</summary>
public sealed class InMemoryValidationSchemaStore : IValidationSchemaStore
{
    private readonly ConcurrentDictionary<string, ValidationSchema> _schemas = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<ValidationSchema?> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_schemas.TryGetValue(key, out var schema) ? schema : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ValidationSchema>> ListAsync(string? targetTypeName = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IEnumerable<ValidationSchema> query = _schemas.Values;
        if (!string.IsNullOrWhiteSpace(targetTypeName))
            query = query.Where(s => string.Equals(s.TargetTypeName, targetTypeName, StringComparison.Ordinal));

        return Task.FromResult<IReadOnlyList<ValidationSchema>>(query.OrderBy(s => s.Key, StringComparer.Ordinal).ToArray());
    }

    /// <inheritdoc />
    public Task SaveAsync(ValidationSchema schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(schema);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schema.Key);
        ArgumentHelpers.ThrowIfNull(schema.Constraints);
        ct.ThrowIfCancellationRequested();
        _schemas[schema.Key] = schema;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_schemas.TryRemove(key, out _));
    }
}
