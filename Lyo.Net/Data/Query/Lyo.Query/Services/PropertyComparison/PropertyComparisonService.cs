using System.Linq.Expressions;
using System.Reflection;
using Lyo.Cache;

namespace Lyo.Query.Services.PropertyComparison;

public sealed class PropertyComparisonService(ICacheService cache, CacheOptions cacheOptions) : IPropertyComparisonService
{
    public Dictionary<string, object?> GetPropertyDifferences<TEntity, TOther>(TEntity entity, TOther newData)
    {
        var entityType = typeof(TEntity);
        var requestType = typeof(TOther);
        var cacheKey = $"PropertyComparison_{entityType.FullName}_{requestType.FullName}";
        var comparisons = cache.GetOrSet<PropertyComparisonInfo[]>(cacheKey, _ => BuildComparisonInfo(entityType, requestType), cacheOptions.ComparisonInfoExpiration);
        var differences = new Dictionary<string, object?>(comparisons.Length);
        foreach (var comparison in comparisons) {
            var currentValue = comparison.EntityGetter(entity!);
            var newValue = comparison.RequestGetter(newData!);
            if (!AreValuesEqualFast(currentValue, newValue, comparison.Strategy, comparison.ConversionType))
                differences[comparison.PropertyName] = newValue;
        }

        return differences;
    }

    private PropertyComparisonInfo[] BuildComparisonInfo(Type entityType, Type requestType)
    {
        var entityProperties = GetCachedProperties(entityType);
        var requestProperties = GetCachedProperties(requestType);
        var comparisons = new List<PropertyComparisonInfo>();
        foreach (var requestProp in requestProperties.Values) {
            if (!requestProp.CanRead)
                continue;

            if (!entityProperties.TryGetValue(requestProp.Name, out var entityProp) || !entityProp.CanWrite)
                continue;

            var entityMetadata = GetTypeMetadata(entityProp.PropertyType);
            var requestMetadata = GetTypeMetadata(requestProp.PropertyType);
            var entityGetter = CreatePropertyGetter(entityProp);
            var requestGetter = CreatePropertyGetter(requestProp);
            var strategy = DetermineComparisonStrategy(entityMetadata, requestMetadata, entityProp.PropertyType, requestProp.PropertyType);
            var conversionType = GetConversionType(strategy, entityMetadata, requestMetadata);
            comparisons.Add(new(requestProp.Name, entityGetter, requestGetter, strategy, conversionType));
        }

        return comparisons.ToArray();
    }

    private static bool AreValuesEqualFast(object? currentValue, object? newValue, ComparisonStrategy strategy, Type? conversionType)
    {
        if (ReferenceEquals(currentValue, newValue))
            return true;

        if (currentValue == null || newValue == null)
            return false;

        return strategy switch {
            ComparisonStrategy.Direct or ComparisonStrategy.EnumToEnum => currentValue.Equals(newValue),
            ComparisonStrategy.EntityStringToRequestEnum => CompareStringToEnum(currentValue, newValue, conversionType!),
            ComparisonStrategy.EntityEnumToRequestString => CompareEnumToString(currentValue, newValue),
            ComparisonStrategy.Convert => CompareWithConversion(currentValue, newValue, conversionType!),
            var _ => currentValue.Equals(newValue)
        };
    }

    private static bool CompareStringToEnum(object stringValue, object enumValue, Type enumType)
    {
        try {
            if (stringValue is not string str)
                return false;

            return Enum.TryParse(enumType, str, true, out var parsedEnum) && parsedEnum.Equals(enumValue);
        }
        catch {
            return false;
        }
    }

    private static bool CompareEnumToString(object enumValue, object stringValue)
    {
        try {
            if (stringValue is not string str)
                return false;

            var enumAsString = enumValue.ToString();
            return string.Equals(enumAsString, str, StringComparison.OrdinalIgnoreCase);
        }
        catch {
            return false;
        }
    }

    private static bool CompareWithConversion(object currentValue, object newValue, Type targetType)
    {
        try {
            var converted = Convert.ChangeType(newValue, targetType);
            return currentValue.Equals(converted);
        }
        catch {
            return false;
        }
    }

    private static ComparisonStrategy DetermineComparisonStrategy(
        PropertyComparisonTypeMetadata entityMetadata,
        PropertyComparisonTypeMetadata requestMetadata,
        Type entityType,
        Type requestType)
    {
        if (entityType == requestType)
            return ComparisonStrategy.Direct;

        if (entityMetadata.IsEnum && requestMetadata.IsEnum && entityMetadata.UnderlyingEnumType == requestMetadata.UnderlyingEnumType)
            return ComparisonStrategy.EnumToEnum;

        if (!entityMetadata.IsEnum && requestMetadata.IsEnum && entityType == typeof(string))
            return ComparisonStrategy.EntityStringToRequestEnum;

        if (entityMetadata.IsEnum && !requestMetadata.IsEnum && requestType == typeof(string))
            return ComparisonStrategy.EntityEnumToRequestString;

        return ComparisonStrategy.Convert;
    }

    private Dictionary<string, PropertyInfo> GetCachedProperties(Type type)
    {
        var cacheKey = $"PropertyInfo_{type.FullName}";
        return cache.GetOrSet<Dictionary<string, PropertyInfo>>(
            cacheKey, _ => type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase),
            cacheOptions.PropertyInfoExpiration);
    }

    private PropertyComparisonTypeMetadata GetTypeMetadata(Type type)
    {
        var cacheKey = $"TypeMetadata_{type.FullName}";
        return cache.GetOrSet<PropertyComparisonTypeMetadata>(
            cacheKey, _ => {
                var isEnum = type.IsEnum;
                var isNullableEnum = IsNullableEnum(type);
                var underlyingEnumType = isEnum ? type : isNullableEnum ? type.GetGenericArguments()[0] : null;
                var underlyingType = isNullableEnum ? type.GetGenericArguments()[0] : type;
                return new(isEnum, isNullableEnum, underlyingEnumType, underlyingType);
            }, cacheOptions.TypeMetadataExpiration);
    }

    private Func<object, object?> CreatePropertyGetter(PropertyInfo property)
    {
        var cacheKey = $"PropertyGetter_{property.DeclaringType?.FullName}_{property.Name}";
        return cache.GetOrSet<Func<object, object?>>(
            cacheKey, _ => {
                var parameter = Expression.Parameter(typeof(object), "obj");
                var cast = Expression.Convert(parameter, property.DeclaringType!);
                var propertyAccess = Expression.Property(cast, property);
                var convert = Expression.Convert(propertyAccess, typeof(object));
                return Expression.Lambda<Func<object, object?>>(convert, parameter).Compile();
            }, cacheOptions.PropertyGetterExpiration);
    }

    private static Type? GetConversionType(ComparisonStrategy strategy, PropertyComparisonTypeMetadata entityMetadata, PropertyComparisonTypeMetadata requestMetadata)
        => strategy switch {
            ComparisonStrategy.EntityStringToRequestEnum => requestMetadata.UnderlyingEnumType,
            ComparisonStrategy.Convert => entityMetadata.UnderlyingType,
            var _ => null
        };

    private static bool IsNullableEnum(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0].IsEnum;
}