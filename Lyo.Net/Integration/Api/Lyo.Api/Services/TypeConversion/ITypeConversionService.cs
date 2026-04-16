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
}