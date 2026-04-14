using System.Collections;
using System.Text.Json;
using Lyo.Cache;

namespace Lyo.Query.Services.ValueConversion;

internal record TypeConversionMetadata(Type UnderlyingType, bool IsEnum, bool IsNullable, Type? EnumType);

public sealed class ValueConversionService(ICacheService cache, CacheOptions cacheOptions) : IValueConversionService
{
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

        if (obj is string or byte[])
            return false;

        return obj is IEnumerable;
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
            }, cacheOptions.TypeMetadataExpiration);
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
            if (Enum.TryParse(enumType, stringValue, true, out var enumResult))
                return enumResult!;

            throw new InvalidOperationException($"Cannot convert string '{stringValue}' to enum type {enumType.Name}");
        }

        try {
            return Enum.ToObject(enumType, value);
        }
        catch (Exception ex) {
            throw new InvalidOperationException($"Cannot convert value '{value}' to enum type {enumType.Name}", ex);
        }
    }
}