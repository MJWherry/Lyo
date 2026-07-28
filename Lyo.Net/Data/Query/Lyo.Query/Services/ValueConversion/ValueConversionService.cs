using Lyo.Common.Conversion;

namespace Lyo.Query.Services.ValueConversion;

/// <summary>Converts filter literal values to CLR types for query parsing. Delegates to the shared <see cref="TypeConversion" /> engine in Lyo.Common.</summary>
public sealed class ValueConversionService : IValueConversionService
{
    /// <inheritdoc />
    public object? ConvertToTargetType(object? value, Type targetType) => TypeConversion.ConvertTo(value, targetType);

    /// <inheritdoc />
    public Type GetUnderlyingType(Type type) => TypeConversion.GetUnderlyingType(type);

    /// <inheritdoc />
    public bool IsObjectEnumerable(object? obj) => TypeConversion.IsObjectEnumerable(obj);
}