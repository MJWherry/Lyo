using System.Collections;
using System.Text.Json;
using Lyo.Query.Services.ValueConversion;

namespace Lyo.Query.Tests;

public class TestValueConversionService : IValueConversionService
{
    public object? ConvertToTargetType(object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (targetType.IsInstanceOfType(value))
            return value;

        if (value is JsonElement je) {
            if (targetType == typeof(string))
                return je.GetString();

            if (je.ValueKind == JsonValueKind.Number && (targetType == typeof(int) || targetType == typeof(int?)))
                return je.GetInt32();

            if (je.ValueKind == JsonValueKind.String)
                return je.GetString();
        }

        try {
            return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
        }
        catch {
            return value;
        }
    }

    public Type GetUnderlyingType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    public bool IsObjectEnumerable(object? obj) => obj is IEnumerable && obj is not string && obj is not byte[];
}