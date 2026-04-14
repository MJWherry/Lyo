namespace Lyo.Query.Services.PropertyComparison;

public interface IPropertyComparisonService
{
    Dictionary<string, object?> GetPropertyDifferences<TEntity, TOther>(TEntity entity, TOther newData);
}