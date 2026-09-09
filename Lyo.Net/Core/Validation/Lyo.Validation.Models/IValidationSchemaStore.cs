namespace Lyo.Validation.Models;

/// <summary>Loads and saves <see cref="ValidationSchema" /> documents. Implementations may be in-memory, PostgreSQL, or a host HTTP client.</summary>
/// <remarks>Read-only backends (for example a fetch-only API client) may throw <see cref="NotSupportedException" /> from <see cref="SaveAsync" /> and <see cref="DeleteAsync" />.</remarks>
public interface IValidationSchemaStore
{
    /// <summary>Returns the schema with <paramref name="key" />, or <c>null</c> when it is missing.</summary>
    Task<ValidationSchema?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Returns stored schemas, optionally filtered by <see cref="ValidationSchema.TargetTypeName" />.</summary>
    Task<IReadOnlyList<ValidationSchema>> ListAsync(string? targetTypeName = null, CancellationToken ct = default);

    /// <summary>Inserts or overwrites the schema identified by <see cref="ValidationSchema.Key" />.</summary>
    Task SaveAsync(ValidationSchema schema, CancellationToken ct = default);

    /// <summary>Removes the schema with <paramref name="key" />. Returns <c>true</c> when a row was deleted.</summary>
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}
