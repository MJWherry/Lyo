using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Finds entities and key metadata from a DbContext for dynamic endpoint registration.</summary>
public static class DynamicEndpointMapper
{
    private const BindingFlags PropertyFlags = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Returns entity types from DbContext by reflecting on DbSet properties.</summary>
    public static IReadOnlyList<Type> GetEntityTypesFromDbContext<TContext>()
        where TContext : DbContext
    {
        var dbSetType = typeof(DbSet<>);
        return typeof(TContext).GetProperties(PropertyFlags)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == dbSetType)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Distinct()
            .ToList();
    }

    /// <summary>Returns primary key info from the EF model. Returns (keyPropertyName, keyClrType) or null if composite or missing.</summary>
    public static (string Name, Type ClrType)? GetPrimaryKeyInfo<TContext, TEntity>(TContext context)
        where TContext : DbContext where TEntity : class
        => GetPrimaryKeyInfo(context, typeof(TEntity));

    /// <summary>Returns primary key info from the EF model by entity type. Returns (keyPropertyName, keyClrType) or null if composite or missing.</summary>
    public static (string Name, Type ClrType)? GetPrimaryKeyInfo(DbContext context, Type entityType)
    {
        var entityTypeConfig = context.Model.FindEntityType(entityType);
        var pk = entityTypeConfig?.FindPrimaryKey();
        if (pk == null || pk.Properties.Count != 1)
            return null;

        var prop = pk.Properties[0];
        return (prop.Name, prop.ClrType);
    }

    /// <summary>Builds an Expression&lt;Func&lt;TEntity, object?&gt;&gt; for default order by the key property.</summary>
    public static Expression<Func<TEntity, object?>> BuildDefaultOrderExpression<TEntity>(string keyPropertyName)
        where TEntity : class
    {
        var param = Expression.Parameter(typeof(TEntity), "x");
        var prop = Expression.Property(param, keyPropertyName);
        var converted = Expression.Convert(prop, typeof(object));
        return Expression.Lambda<Func<TEntity, object?>>(converted, param);
    }

    /// <summary>Builds the default order expression for a given entity type (reflection-based).</summary>
    public static LambdaExpression BuildDefaultOrderExpression(Type entityType, string keyPropertyName)
    {
        var param = Expression.Parameter(entityType, "x");
        var prop = Expression.Property(param, keyPropertyName);
        var converted = Expression.Convert(prop, typeof(object));
        var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(object));
        return Expression.Lambda(funcType, converted, param);
    }

    /// <summary>Converts an entity type name to a PascalCase route segment (for example PersonEntity -&gt; PersonEntity).</summary>
    public static string ToRouteSegment(Type entityType) => entityType.Name;
}