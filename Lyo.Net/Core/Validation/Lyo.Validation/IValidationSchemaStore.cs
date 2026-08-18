namespace Lyo.Validation;

/// <summary>Loads and persists <see cref="ValidationSchema" /> documents. Implementations may be in-memory, PostgreSQL, or a host HTTP client.</summary>
/// <remarks>Read-only backends (for example a fetch-only API client) may throw <see cref="NotSupportedException" /> from <see cref="SaveAsync" /> and <see cref="DeleteAsync" />.</remarks>
public interface IValidationSchemaStore
{
    /// <summary>Gets the schema with <paramref name="key" />, or <c>null</c> when it does not exist.</summary>
    Task<ValidationSchema?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Lists stored schemas, optionally filtered by <see cref="ValidationSchema.TargetTypeName" />.</summary>
    Task<IReadOnlyList<ValidationSchema>> ListAsync(string? targetTypeName = null, CancellationToken ct = default);

    /// <summary>Inserts or replaces the schema identified by <see cref="ValidationSchema.Key" />.</summary>
    Task SaveAsync(ValidationSchema schema, CancellationToken ct = default);

    /// <summary>Deletes the schema with <paramref name="key" />. Returns <c>true</c> when a row was removed.</summary>
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}
