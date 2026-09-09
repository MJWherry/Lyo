namespace Lyo.Query.Services.PropertyComparison;

/// <summary>
/// Compares public instance properties that exist on both the entity type and the incoming data type, and returns the names whose values differ. Used for patch and
/// update diffs.
/// </summary>
public interface IPropertyComparisonService
{
    /// <summary>
    /// For each writable property on <typeparamref name="TEntity" /> that has a same-named readable property on <typeparamref name="TOther" />, compares current and new
    /// values with an inferred strategy (direct, enum/string, or <see cref="Convert.ChangeType(object?, Type)" />).
    /// </summary>
    /// <typeparam name="TEntity">Persisted or domain entity type.</typeparam>
    /// <typeparam name="TOther">Incoming DTO or request type.</typeparam>
    /// <param name="entity">Current entity instance.</param>
    /// <param name="newData">Incoming data. Readable properties are read from it.</param>
    /// <returns>Property name to proposed new value (from <paramref name="newData" />) for properties that differ. Equal properties are omitted.</returns>
    Dictionary<string, object?> GetPropertyDifferences<TEntity, TOther>(TEntity entity, TOther newData);
}