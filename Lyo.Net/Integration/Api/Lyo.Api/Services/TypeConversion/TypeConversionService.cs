using Lyo.Cache;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.TypeConversion;

/// <inheritdoc cref="ITypeConversionService" />
public sealed class TypeConversionService(ICacheService cache, CacheOptions cacheOptions) : ITypeConversionService
{
    public IReadOnlyList<string> GetPrimaryKeyPropertyNames<TEntity>(DbContext context)
    {
        var keyMetadata = GetEntityKeyMetadataCached<TEntity>(context);
        var names = new string[keyMetadata.Properties.Count];
        for (var i = 0; i < keyMetadata.Properties.Count; i++)
            names[i] = keyMetadata.Properties[i].Name;

        return names;
    }

    public IReadOnlyList<object?> GetPrimaryKeyValues<TEntity>(TEntity entity, DbContext context)
    {
        ArgumentHelpers.ThrowIfNull(entity);
        var keyMetadata = GetEntityKeyMetadataCached<TEntity>(context);
        var values = new List<object?>(keyMetadata.ExpectedKeyCount);
        values.AddRange(keyMetadata.Properties.Select(property => property.PropertyInfo!.GetValue(entity)));
        return values;
    }

    public IReadOnlyList<object?> GetPrimaryKeyValues(object entity, DbContext context)
    {
        ArgumentHelpers.ThrowIfNull(entity);
        var entry = context.Entry(entity);
        var key = entry.Metadata.FindPrimaryKey();
        OperationHelpers.ThrowIfNull(key, $"No primary key defined for {entry.Metadata.Name}");
        var values = new object?[key.Properties.Count];
        for (var i = 0; i < key.Properties.Count; i++)
            values[i] = entry.Property(key.Properties[i].Name).CurrentValue;

        return values;
    }

    public object[] ConvertKeysForFind<TEntity>(object[] keys, DbContext context)
    {
        var keyMetadata = GetEntityKeyMetadataCached<TEntity>(context);
        ArgumentHelpers.ThrowIf(keys.Length != keyMetadata.ExpectedKeyCount, $"Expected {keyMetadata.ExpectedKeyCount} key value(s), but got {keys.Length}");
        var convertedKeys = new object[keys.Length];
        for (var i = 0; i < keys.Length; i++) {
            var keyProperty = keyMetadata.Properties[i];
            var keyValue = keys[i];
            convertedKeys[i] = ConvertToTargetType(keyValue, keyProperty.ClrType)!;
        }

        return convertedKeys;
    }

    public IReadOnlyList<object?>? TryGetPrimaryKeyValuesFromProjectedDictionary(IReadOnlyDictionary<string, object?> row, Type entityClrType, DbContext context)
    {
        ArgumentHelpers.ThrowIfNull(row);
        ArgumentHelpers.ThrowIfNull(entityClrType);
        ArgumentHelpers.ThrowIfNull(context);
        var entityType = context.Model.FindEntityType(entityClrType);
        var pk = entityType?.FindPrimaryKey();
        if (pk is null || pk.Properties.Count == 0)
            return null;

        var values = new object?[pk.Properties.Count];
        for (var i = 0; i < pk.Properties.Count; i++) {
            var propertyName = pk.Properties[i].Name;
            if (!TryGetProjectedRowValue(row, propertyName, out values[i]))
                return null;
        }

        return values;
    }

    public object? ConvertToTargetType(object? value, Type targetType) => Common.Conversion.TypeConversion.ConvertTo(value, targetType);

    public Type GetUnderlyingType(Type type) => Common.Conversion.TypeConversion.GetUnderlyingType(type);

    public bool IsObjectEnumerable(object? obj) => Common.Conversion.TypeConversion.IsObjectEnumerable(obj);

    private static bool TryGetProjectedRowValue(IReadOnlyDictionary<string, object?> row, string propertyName, out object? value)
    {
        if (row.TryGetValue(propertyName, out value))
            return true;

        foreach (var kv in row) {
            if (string.Equals(kv.Key, propertyName, StringComparison.OrdinalIgnoreCase)) {
                value = kv.Value;
                return true;
            }

            var suffix = "." + propertyName;
            if (kv.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) {
                value = kv.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private EntityKeyMetadata GetEntityKeyMetadataCached<TEntity>(DbContext context)
    {
        var cacheKey = $"EntityKeyMetadata_{typeof(TEntity).FullName}";
        return cache.GetOrSet<EntityKeyMetadata>(
            cacheKey, _ => {
                var entityType = context.Model.FindEntityType(typeof(TEntity));
                OperationHelpers.ThrowIfNull(entityType, $"Entity type {typeof(TEntity).Name} not found in model");
                var primaryKey = entityType.FindPrimaryKey();
                OperationHelpers.ThrowIfNull(primaryKey, $"No primary key defined for {typeof(TEntity).Name}");
                var keyProperties = primaryKey.Properties.ToArray();
                return new(keyProperties, primaryKey.Properties.Count);
            }, cacheOptions.TypeMetadataExpiration)!;
    }
}