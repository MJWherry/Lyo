using Lyo.Query.Services.ValueConversion;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.TypeConversion;

/// <summary>Extends IValueConversionService with EF Core-specific methods for primary key handling.</summary>
public interface ITypeConversionService : IValueConversionService
{
    IReadOnlyList<string> GetPrimaryKeyPropertyNames<TEntity>(DbContext context);

    IReadOnlyList<object?> GetPrimaryKeyValues<TEntity>(TEntity entity, DbContext context);

    /// <summary>Primary key values for any tracked or loaded entity instance (uses EF metadata; supports proxies).</summary>
    IReadOnlyList<object?> GetPrimaryKeyValues(object entity, DbContext context);

    /// <summary>Converts an object array of key values to properly typed objects for EF Core FindAsync</summary>
    object[] ConvertKeysForFind<TEntity>(object[] keys, DbContext context);

    /// <summary>
    /// Reads primary key columns from a projected query row (<see cref="System.Collections.Generic.Dictionary{TKey,TValue}" />)
    /// when they are present as top-level keys (or keys ending with <c>.PropertyName</c>). Returns null if any key segment is missing.
    /// </summary>
    IReadOnlyList<object?>? TryGetPrimaryKeyValuesFromProjectedDictionary(IReadOnlyDictionary<string, object?> row, Type entityClrType, DbContext context);
}