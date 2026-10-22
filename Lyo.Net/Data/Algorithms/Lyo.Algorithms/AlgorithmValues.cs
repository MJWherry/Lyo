using System.Globalization;
using System.Text.Json;
using Lyo.Common.Core.Conversion;

namespace Lyo.Algorithms;

/// <summary>Compile-time JSON conversion. Execute does not call this.</summary>
internal static class AlgorithmValues
{
    public static T Convert<T>(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return AlgorithmHelpers.Throw<T>("Algorithm value is missing.", "algorithm.payload.invalid");

        if (typeof(T) == typeof(decimal) && element.ValueKind == JsonValueKind.Number)
            return (T)(object)element.GetDecimal();

        if (typeof(T) == typeof(double) && element.ValueKind == JsonValueKind.Number)
            return (T)(object)element.GetDouble();

        if (!TypeConversion.TryFromJsonElement<T>(element, out var value) || value is null)
            return AlgorithmHelpers.Throw<T>($"Algorithm value cannot be read as {typeof(T).Name}.", "algorithm.payload.invalid");

        return value;
    }

    public static decimal Decimal(JsonElement element)
    {
        AlgorithmHelpers.ThrowIf(element.ValueKind != JsonValueKind.Number, "Algorithm number is required.", "algorithm.payload.invalid");
        return element.GetDecimal();
    }

    public static double Double(JsonElement element)
    {
        AlgorithmHelpers.ThrowIf(element.ValueKind != JsonValueKind.Number, "Algorithm number is required.", "algorithm.payload.invalid");
        return element.GetDouble();
    }

    public static decimal WeightDecimal(JsonElement element) => element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? 1m : Decimal(element);

    public static double WeightDouble(JsonElement element) => element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? 1d : Double(element);

    public static void ThrowIfNotIntegral(JsonElement element, Type inputType)
    {
        if (!AlgorithmTypeBinding.IsIntegral(inputType))
            return;

        var number = Decimal(element);
        AlgorithmHelpers.ThrowIf(number != decimal.Truncate(number), $"'{number.ToString(CultureInfo.InvariantCulture)}' is not an integer.", "algorithm.payload.invalid");
    }

    public static TResult Zero<TResult>() => default!;

    public static double Points<T>(T value)
    {
        if (value is double number)
            return number;

        if (value is float single)
            return single;

        if (value is decimal money)
            return (double)money;

        if (value is int i32)
            return i32;

        if (value is long i64)
            return i64;

        if (value is short i16)
            return i16;

        if (value is byte u8)
            return u8;

        if (value is sbyte i8)
            return i8;

        if (value is uint u32)
            return u32;

        if (value is ulong u64)
            return u64;

        if (value is ushort u16)
            return u16;

        return 0;
    }

    public static object? Box<T>(T value) => AlgorithmTypeBinding.IsNumeric(typeof(T)) ? null : value;
}
