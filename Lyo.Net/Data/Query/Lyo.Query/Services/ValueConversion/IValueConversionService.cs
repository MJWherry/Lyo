namespace Lyo.Query.Services.ValueConversion;

/// <summary>
/// Minimal interface for value conversion used by query filtering. Does not depend on EF Core. Lyo.Api's ITypeConversionService extends this with EF-specific methods
/// (GetPrimaryKeyValues, etc.).
/// </summary>
public interface IValueConversionService
{
    /// <summary>Converts a single value to the target type</summary>
    object? ConvertToTargetType(object? value, Type targetType);

    /// <summary>Gets the underlying type (handles nullable types)</summary>
    Type GetUnderlyingType(Type type);

    /// <summary>Checks if an object is enumerable (excluding string and byte[])</summary>
    bool IsObjectEnumerable(object? obj);
}