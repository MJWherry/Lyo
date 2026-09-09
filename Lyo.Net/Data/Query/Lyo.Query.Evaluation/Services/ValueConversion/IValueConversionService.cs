namespace Lyo.Query.Services.ValueConversion;

/// <summary>
/// Narrow conversion surface used by query filtering. Does not depend on EF Core. Lyo.Api's ITypeConversionService extends this with EF-specific members
/// (GetPrimaryKeyValues, etc.).
/// </summary>
public interface IValueConversionService
{
    /// <summary>Converts one value to <paramref name="targetType" /> (JSON elements, enums, nullable wrappers, and common primitives).</summary>
    /// <param name="value">Incoming value (may be null).</param>
    /// <param name="targetType">CLR type to convert to.</param>
    /// <returns>Converted value, or null when <paramref name="value" /> is null.</returns>
    /// <exception cref="InvalidOperationException">Raised when the value cannot be converted to <paramref name="targetType" />.</exception>
    object? ConvertToTargetType(object? value, Type targetType);

    /// <summary>Underlying non-nullable type for nullable value types; otherwise <paramref name="type" />.</summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>Underlying type.</returns>
    Type GetUnderlyingType(Type type);

    /// <summary>True when <paramref name="obj" /> is a non-string, non-byte[] <see cref="System.Collections.IEnumerable" />.</summary>
    /// <param name="obj">Object to test.</param>
    /// <returns><c>true</c> if <paramref name="obj" /> is an enumerable collection (excluding <see cref="string" /> and <c>byte[]</c>).</returns>
    bool IsObjectEnumerable(object? obj);
}