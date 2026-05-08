namespace Lyo.Query.Services.ValueConversion;

/// <summary>
/// Minimal interface for value conversion used by query filtering. Does not depend on EF Core. Lyo.Api's ITypeConversionService extends this with EF-specific methods
/// (GetPrimaryKeyValues, etc.).
/// </summary>
public interface IValueConversionService
{
    /// <summary>Converts a single value to <paramref name="targetType" /> (handles JSON elements, enums, nullable wrappers, and common primitives).</summary>
    /// <param name="value">The incoming value (may be null).</param>
    /// <param name="targetType">The CLR type to convert to.</param>
    /// <returns>The converted value, or null when <paramref name="value" /> is null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to <paramref name="targetType" />.</exception>
    object? ConvertToTargetType(object? value, Type targetType);

    /// <summary>Returns the underlying non-nullable type for nullable value types; otherwise returns <paramref name="type" />.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The underlying type.</returns>
    Type GetUnderlyingType(Type type);

    /// <summary>Determines whether <paramref name="obj" /> is a non-string, non-byte[] <see cref="System.Collections.IEnumerable" />.</summary>
    /// <param name="obj">The object to test.</param>
    /// <returns><c>true</c> if <paramref name="obj" /> is an enumerable collection (excluding <see cref="string" /> and <c>byte[]</c>).</returns>
    bool IsObjectEnumerable(object? obj);
}