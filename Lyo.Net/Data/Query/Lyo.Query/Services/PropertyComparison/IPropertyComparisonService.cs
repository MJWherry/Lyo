namespace Lyo.Query.Services.PropertyComparison;

/// <summary>
/// Compares public instance properties that exist on both the entity type and the incoming data type and returns property names where values differ (for patch/update diffing).
/// </summary>
public interface IPropertyComparisonService
{
    /// <summary>
    /// For each writable property on <typeparamref name="TEntity" /> that has a same-named readable property on <typeparamref name="TOther" />, compares current vs new
    /// values using an inferred strategy (direct, enum/string, or <see cref="Convert.ChangeType(object?, Type)" />).
    /// </summary>
    /// <typeparam name="TEntity">The persisted or domain entity type.</typeparam>
    /// <typeparam name="TOther">The incoming DTO or request type.</typeparam>
    /// <param name="entity">The current entity instance.</param>
    /// <param name="newData">The incoming data; readable properties are read.</param>
    /// <returns>
    /// A dictionary of property name to proposed new value (from <paramref name="newData" />) for properties that differ. Properties that are equal are omitted.
    /// </returns>
    Dictionary<string, object?> GetPropertyDifferences<TEntity, TOther>(TEntity entity, TOther newData);
}