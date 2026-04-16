using System.Collections;
using System.Text.Json;
using Lyo.Cache;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.TypeConversion;

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
        ArgumentHelpers.ThrowIfNull(entity, nameof(entity));
        var keyMetadata = GetEntityKeyMetadataCached<TEntity>(context);
        var values = new List<object?>(keyMetadata.ExpectedKeyCount);
        values.AddRange(keyMetadata.Properties.Select(property => property.PropertyInfo!.GetValue(entity)));
        return values;
    }

    public IReadOnlyList<object?> GetPrimaryKeyValues(object entity, DbContext context)
    {
        ArgumentHelpers.ThrowIfNull(entity, nameof(entity));
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

    public object? ConvertToTargetType(object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (targetType.IsInstanceOfType(value))
            return value;

        var metadata = GetTypeConversionMetadataCached(targetType);
        if (value is byte[])
            return value;

        if (value is JsonElement element)
            return ConvertJsonElementToType(element, targetType, metadata);

        if (metadata.UnderlyingType == typeof(DateOnly)) {
            if (value is string dateString && DateOnly.TryParse(dateString, out var dateOnly))
                return dateOnly;

            throw new InvalidOperationException($"Cannot convert value '{value}' to DateOnly");
        }

        if (metadata.UnderlyingType == typeof(Guid)) {
            if (value is string guidString && Guid.TryParse(guidString, out var guid))
                return guid;

            throw new InvalidOperationException($"Cannot convert value '{value}' to Guid");
        }

        if (metadata.IsEnum)
            return ConvertToEnum(value, metadata.EnumType!);

        if (value is string str)
            return ConvertStringToType(str, metadata.UnderlyingType);

        try {
            return Convert.ChangeType(value, metadata.UnderlyingType);
        }
        catch (Exception ex) {
            throw new InvalidOperationException($"Cannot convert value '{value}' of type '{value.GetType().Name}' to type '{targetType.Name}'.", ex);
        }
    }

    public Type GetUnderlyingType(Type type) => GetTypeConversionMetadataCached(type).UnderlyingType;

    public bool IsObjectEnumerable(object? obj)
    {
        if (obj == null)
            return false;

        // Fast path for common non-enumerable types
        if (obj is string or byte[])
            return false;

        return obj is IEnumerable;
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

    private TypeConversionMetadata GetTypeConversionMetadataCached(Type type)
    {
        var cacheKey = $"TypeConversion_{type.FullName}";
        return cache.GetOrSet<TypeConversionMetadata>(
            cacheKey, _ => {
                var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
                var isNullable = Nullable.GetUnderlyingType(type) != null;
                var isEnum = underlyingType.IsEnum;
                return new(underlyingType, isEnum, isNullable, isEnum ? underlyingType : null);
            }, cacheOptions.TypeMetadataExpiration)!;
    }

    private static object ConvertJsonElementToType(JsonElement element, Type targetType, TypeConversionMetadata metadata)
        => Type.GetTypeCode(metadata.UnderlyingType) switch {
            TypeCode.Boolean => element.GetBoolean(),
            TypeCode.Byte => element.GetByte(),
            TypeCode.SByte => element.GetSByte(),
            TypeCode.Int16 => element.GetInt16(),
            TypeCode.UInt16 => element.GetUInt16(),
            TypeCode.Int32 => element.GetInt32(),
            TypeCode.UInt32 => element.GetUInt32(),
            TypeCode.Int64 => element.GetInt64(),
            TypeCode.UInt64 => element.GetUInt64(),
            TypeCode.Single => element.GetSingle(),
            TypeCode.Double => element.GetDouble(),
            TypeCode.Decimal => element.GetDecimal(),
            TypeCode.DateTime => element.GetDateTime(),
            TypeCode.String => element.GetString()!,
            var _ when metadata.UnderlyingType == typeof(DateTimeOffset) => element.GetDateTimeOffset(),
            var _ => element.Deserialize(targetType) ?? throw new InvalidOperationException($"Cannot convert JSON element to type {targetType.Name}")
        };

    private static object ConvertStringToType(string str, Type underlyingType)
    {
        // Use TypeCode for better performance on common types
        var typeCode = Type.GetTypeCode(underlyingType);
        return typeCode switch {
            TypeCode.Int32 => int.Parse(str),
            TypeCode.Int64 => long.Parse(str),
            TypeCode.Int16 => short.Parse(str),
            TypeCode.Decimal => decimal.Parse(str),
            TypeCode.Double => double.Parse(str),
            TypeCode.Single => float.Parse(str),
            TypeCode.DateTime => DateTime.Parse(str),
            TypeCode.Boolean => bool.Parse(str),
            TypeCode.String => str,
            var _ => underlyingType.Name switch {
                nameof(Guid) => Guid.Parse(str),
                nameof(DateTimeOffset) => DateTimeOffset.Parse(str),
                nameof(DateOnly) => DateOnly.Parse(str),
                var _ => Convert.ChangeType(str, underlyingType)
            }
        };
    }

    private static object ConvertToEnum(object value, Type enumType)
    {
        if (value is string stringValue) {
            return Enum.TryParse(enumType, stringValue, true, out var enumResult)
                ? enumResult
                : throw new InvalidOperationException($"Cannot convert string '{stringValue}' to enum type {enumType.Name}");
        }

        try {
            return Enum.ToObject(enumType, value);
        }
        catch (Exception ex) {
            throw new InvalidOperationException($"Cannot convert value '{value}' to enum type {enumType.Name}", ex);
        }
    }
}