using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Microsoft.AspNetCore.Http;

namespace Lyo.Api;

public static class Extensions
{
    private static readonly HashSet<TypeCode> NumericTypeCodes = new() {
        TypeCode.Byte,
        TypeCode.SByte,
        TypeCode.Int16,
        TypeCode.UInt16,
        TypeCode.Int32,
        TypeCode.UInt32,
        TypeCode.Int64,
        TypeCode.UInt64,
        TypeCode.Single,
        TypeCode.Double,
        TypeCode.Decimal
    };

    public static LyoProblemDetails ApiErrorFromException(Exception ex, string? message = null, string errorCode = Lyo.Api.Models.Constants.ApiErrorCodes.Unknown)
        => LyoProblemDetailsBuilder.CreateWithTrace(Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString())
            .WithErrorCode(errorCode)
            .WithMessage(message ?? ex.Message)
            .AddApiError(errorCode, message ?? ex.Message, ex.StackTrace)
            .Build();

    public static async Task<byte[]> HashAsync(this IFormFile file, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        var stream = file.OpenReadStream();
        try {
            var hashBytes = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
            return hashBytes;
        }
        finally {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static bool ConvertToBoolean(string value)
        => value.ToLowerInvariant() switch {
            "true" or "1" or "yes" or "on" => true,
            "false" or "0" or "no" or "off" => false,
            var _ => bool.TryParse(value, out var result) ? result : throw new FormatException($"Unable to parse '{value}' as Boolean")
        };

    extension(Type type)
    {
        /// <summary>Determines if the type is a numeric type (byte, int, float, decimal, etc.)</summary>
        public bool IsNumericType() => NumericTypeCodes.Contains(Type.GetTypeCode(type));

        /// <summary>Determines if the type is nullable</summary>
        public bool IsNullable() => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>Gets the underlying type if nullable, otherwise returns the original type</summary>
        public Type GetUnderlyingType() => type.IsNullable() ? Nullable.GetUnderlyingType(type)! : type;

        /// <summary> Gets the element type of a collection (array or generic collection)</summary>
        public Type GetCollectionElementType()
        {
            if (type.IsArray)
                return type.GetElementType() ?? typeof(object);

            if (type.IsGenericType && type.GetGenericArguments().Length > 0)
                return type.GetGenericArguments()[0];

            // Check if it implements IEnumerable<T>
            var enumerableInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableInterface?.GetGenericArguments()[0] ?? typeof(object);
        }

        /// <summary>Returns a human-readable type name, resolving generic types like List&lt;T&gt; instead of List`1.</summary>
        public string GetFriendlyTypeName()
        {
            if (!type.IsGenericType)
                return type.Name;

            var baseName = type.Name[..type.Name.IndexOf('`')];
            var args = string.Join(", ", type.GetGenericArguments().Select(a => a.GetFriendlyTypeName()));
            return $"{baseName}<{args}>";
        }

        /// <summary>Checks if type is a collection type (excluding string and byte[])</summary>
        public bool IsCollectionType()
            => type.IsArray || (type != typeof(string) && type != typeof(byte[]) && type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

        private Array CreateArrayOfType(object?[] values)
        {
            var array = Array.CreateInstance(type, values.Length);
            for (var i = 0; i < values.Length; i++)
                array.SetValue(values[i], i);

            return array;
        }
    }

    extension(object? obj)
    {
        /// <summary>Determines if an object is enumerable (but not a string or byte[])</summary>
        public bool IsObjectEnumerable() => obj is not null and not string and not byte[] and IEnumerable;

        /// <summary>Tries to cast an object to IEnumerable{T}, excluding strings and byte arrays</summary>
        public bool TryGetAsEnumerable<T>(out IEnumerable<T> enumerable)
        {
            if (obj is IEnumerable<T> e and not string and not byte[]) {
                enumerable = e;
                return true;
            }

            enumerable = [];
            return false;
        }

        /// <summary>Converts a single value to the specified type with comprehensive type handling</summary>
        public object? ConvertToTargetType(Type targetType)
        {
            if (obj == null) {
                return targetType.IsNullable() || !targetType.IsValueType
                    ? null
                    : throw new ArgumentNullException(nameof(obj), $"Cannot convert null to non-nullable type {targetType.Name}");
            }

            // Handle JsonElement extraction
            if (obj is JsonElement jsonElement)
                obj = jsonElement.ExtractValueFromJsonElement();

            // Handle nullable types
            var underlyingType = targetType.GetUnderlyingType();
            if (targetType.IsNullable() && obj == null)
                return null;

            // Direct type match
            if (obj != null && underlyingType.IsAssignableFrom(obj.GetType()))
                return obj;

            // Use underlying type for conversion
            return obj.ConvertSingleValue(underlyingType);
        }

        /// <summary>Converts an object (single value or collection) to the specified type</summary>
        public object? ConvertToType(Type targetType)
        {
            if (obj == null) {
                return targetType.IsNullable() || !targetType.IsValueType
                    ? null
                    : throw new ArgumentNullException(nameof(obj), $"Cannot convert null to non-nullable type {targetType.Name}");
            }

            // Handle JsonElement extraction
            if (obj is JsonElement jsonElement)
                obj = jsonElement.ExtractValueFromJsonElement();

            // If target is not a collection type, handle as single value
            if (!targetType.IsCollectionType() || targetType == typeof(string) || targetType == typeof(byte[]))
                return obj.ConvertToTargetType(targetType);

            // Handle collection conversion
            var elementType = targetType.GetCollectionElementType();

            // Try to get as enumerable
            if (!obj.TryGetAsEnumerable<object>(out var enumerable)) {
                // Single value - wrap in collection
                var singleConverted = obj.ConvertToTargetType(elementType);
                return elementType.CreateArrayOfType([singleConverted]);
            }

            // Convert each element
            var convertedValues = enumerable.Select(item => item.ConvertToTargetType(elementType)).ToArray();
            return elementType.CreateArrayOfType(convertedValues);
        }

        private object ConvertSingleValue(Type targetType)
        {
            var stringValue = obj?.ToString() ?? string.Empty;
            return targetType.Name switch {
                nameof(Guid) => Guid.TryParse(stringValue, out var guid) ? guid : throw new FormatException($"Unable to parse '{stringValue}' as Guid"),
                nameof(DateTime) => DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    ? dt
                    : throw new FormatException($"Unable to parse '{stringValue}' as DateTime"),
                nameof(DateOnly) => DateOnly.TryParse(stringValue, out var dateOnly) ? dateOnly : throw new FormatException($"Unable to parse '{stringValue}' as DateOnly"),
                nameof(TimeOnly) => TimeOnly.TryParse(stringValue, out var timeOnly) ? timeOnly : throw new FormatException($"Unable to parse '{stringValue}' as TimeOnly"),
                nameof(Boolean) => ConvertToBoolean(stringValue),
                nameof(String) => stringValue,
                var _ => obj.ConvertByTypeCategory(targetType)
            };
        }

        private object ConvertByTypeCategory(Type targetType)
        {
            if (targetType.IsEnum) {
                var stringValue = obj?.ToString()!;
                if (int.TryParse(stringValue, out var intValue) && Enum.IsDefined(targetType, intValue))
                    return Enum.ToObject(targetType, intValue);

                return Enum.Parse(targetType, stringValue, true);
            }

            return targetType.IsNumericType()
                ? Convert.ChangeType(obj, targetType, CultureInfo.InvariantCulture)
                : throw new InvalidOperationException($"Cannot convert '{obj}' of type '{obj.GetType().Name}' to '{targetType.Name}'.");
        }
    }

    extension(JsonElement element)
    {
        /// <summary>Safely extracts a value from a JsonElement</summary>
        public object? ExtractValueFromJsonElement()
            => element.ValueKind switch {
                JsonValueKind.Array => element.ExtractArrayFromJsonElement(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt32(out var i) => i,
                JsonValueKind.Number when element.TryGetInt64(out var l) => l,
                JsonValueKind.Number when element.TryGetDouble(out var d) => d,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                var _ => element.GetRawText()
            };

        /// <summary>Extracts an array of values from a JsonElement array</summary>
        public IEnumerable<object?> ExtractArrayFromJsonElement()
            => element.ValueKind != JsonValueKind.Array
                ? throw new ArgumentException("JsonElement is not an array", nameof(element))
                : element.EnumerateArray().Select(element1 => element1.ExtractValueFromJsonElement()).ToList();
    }
}