using System.Text.Json;
using Lyo.Common.Conversion;
using Microsoft.Extensions.Logging;

namespace Lyo.Common.Tests;

public class TypeConversionTests
{
    public enum SampleEnum
    {
        Unknown = 0,
        First = 1,
        Second = 2
    }

    private static JsonElement ToElement<T>(T value) => JsonSerializer.SerializeToElement(value);

    // ---- ConvertTo: scalars ----

    [Fact]
    public void ConvertTo_Null_ReturnsNull() => Assert.Null(TypeConversion.ConvertTo(null, typeof(int)));

    [Fact]
    public void ConvertTo_SameType_ReturnsSameInstance()
    {
        var value = Guid.NewGuid().ToString();
        Assert.Same(value, TypeConversion.ConvertTo(value, typeof(string)));
    }

    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("42", typeof(long), 42L)]
    [InlineData("42.5", typeof(double), 42.5)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("2", typeof(short), (short)2)]
    public void ConvertTo_StringToPrimitive_Parses(string input, Type targetType, object expected) => Assert.Equal(expected, TypeConversion.ConvertTo(input, targetType));

    [Fact]
    public void ConvertTo_StringToDecimal_Parses() => Assert.Equal(42.5m, TypeConversion.ConvertTo("42.5", typeof(decimal)));

    [Fact]
    public void ConvertTo_StringToGuid_Parses()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, TypeConversion.ConvertTo(guid.ToString(), typeof(Guid)));
    }

    [Fact]
    public void ConvertTo_InvalidGuid_Throws() => Assert.Throws<TypeConversionException>(() => TypeConversion.ConvertTo("not-a-guid", typeof(Guid)));

    [Fact]
    public void ConvertTo_StringToDateOnly_Parses() => Assert.Equal(new DateOnly(2026, 7, 26), TypeConversion.ConvertTo("2026-07-26", typeof(DateOnly)));

    [Fact]
    public void ConvertTo_StringToTimeOnly_Parses() => Assert.Equal(new TimeOnly(13, 30), TypeConversion.ConvertTo("13:30", typeof(TimeOnly)));

    [Fact]
    public void ConvertTo_StringToDateTimeOffset_Parses()
        => Assert.Equal(new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.Zero), TypeConversion.ConvertTo("2026-07-26T00:00:00+00:00", typeof(DateTimeOffset)));

    [Fact]
    public void ConvertTo_NullableTarget_UnwrapsUnderlyingType() => Assert.Equal(42, TypeConversion.ConvertTo("42", typeof(int?)));

    [Fact]
    public void ConvertTo_IntToLong_ChangesType() => Assert.Equal(42L, TypeConversion.ConvertTo(42, typeof(long)));

    [Fact]
    public void ConvertTo_ByteArray_PassesThrough()
    {
        var bytes = new byte[] { 1, 2, 3 };
        Assert.Same(bytes, TypeConversion.ConvertTo(bytes, typeof(byte[])));
    }

    [Fact]
    public void ConvertTo_Unconvertible_ThrowsWithDescriptiveMessage()
    {
        var ex = Assert.Throws<TypeConversionException>(() => TypeConversion.ConvertTo(new(), typeof(int)));
        Assert.Contains("Cannot convert", ex.Message);
    }

    [Fact]
    public void ConvertTo_Generic_ReturnsTypedValue() => Assert.Equal(42, TypeConversion.ConvertTo<int>("42"));

    // ---- ConvertTo: enums ----

    [Theory]
    [InlineData("First", SampleEnum.First)]
    [InlineData("second", SampleEnum.Second)]
    [InlineData("1", SampleEnum.First)]
    public void ConvertTo_EnumFromString_Parses(string input, SampleEnum expected) => Assert.Equal(expected, TypeConversion.ConvertTo(input, typeof(SampleEnum)));

    [Fact]
    public void ConvertTo_EnumFromNumber_Converts() => Assert.Equal(SampleEnum.Second, TypeConversion.ConvertTo(2, typeof(SampleEnum)));

    [Fact]
    public void ConvertTo_EnumInvalidName_Throws() => Assert.Throws<TypeConversionException>(() => TypeConversion.ConvertTo("nope", typeof(SampleEnum)));

    [Fact]
    public void ConvertTo_NullableEnum_Parses() => Assert.Equal(SampleEnum.First, TypeConversion.ConvertTo("First", typeof(SampleEnum?)));

    // ---- ConvertTo: booleans ----

    [Fact]
    public void ConvertTo_StrictBoolean_RejectsLenientTokens() => Assert.ThrowsAny<Exception>(() => TypeConversion.ConvertTo("yes", typeof(bool)));

    [Theory]
    [InlineData("yes", true)]
    [InlineData("on", true)]
    [InlineData("1", true)]
    [InlineData("t", true)]
    [InlineData("Y", true)]
    [InlineData("no", false)]
    [InlineData("off", false)]
    [InlineData("0", false)]
    [InlineData("f", false)]
    [InlineData("N", false)]
    public void ConvertTo_LenientBoolean_AcceptsExtraTokens(string input, bool expected) => Assert.Equal(expected, TypeConversion.ConvertTo(input, typeof(bool), true));

    // ---- ConvertTo: JsonElement ----

    [Fact]
    public void ConvertTo_JsonNumber_ToInt() => Assert.Equal(42, TypeConversion.ConvertTo(ToElement(42), typeof(int)));

    [Fact]
    public void ConvertTo_JsonString_ToInt_FallsBackToStringParse() => Assert.Equal(42, TypeConversion.ConvertTo(ToElement("42"), typeof(int)));

    [Fact]
    public void ConvertTo_JsonString_ToGuid()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, TypeConversion.ConvertTo(ToElement(guid.ToString()), typeof(Guid)));
    }

    [Fact]
    public void ConvertTo_JsonNull_ReturnsNull() => Assert.Null(TypeConversion.ConvertTo(JsonSerializer.SerializeToElement<string?>(null), typeof(int?)));

    [Fact]
    public void ConvertTo_JsonEnumName_Parses() => Assert.Equal(SampleEnum.Second, TypeConversion.ConvertTo(ToElement("Second"), typeof(SampleEnum)));

    [Fact]
    public void ConvertTo_JsonEnumNumber_Parses() => Assert.Equal(SampleEnum.Second, TypeConversion.ConvertTo(ToElement(2), typeof(SampleEnum)));

    [Fact]
    public void ConvertTo_JsonObject_DeserializesComplexType()
    {
        var element = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["key"] = "value" });
        var result = TypeConversion.ConvertTo(element, typeof(Dictionary<string, string>));
        var dict = Assert.IsType<Dictionary<string, string>>(result);
        Assert.Equal("value", dict["key"]);
    }

    // ---- FromJsonElement (loose extraction) ----

    [Fact]
    public void FromJsonElement_String_ReturnsString() => Assert.Equal("abc", TypeConversion.FromJsonElement(ToElement("abc")));

    [Fact]
    public void FromJsonElement_IntNumber_ReturnsInt() => Assert.Equal(42, TypeConversion.FromJsonElement(ToElement(42)));

    [Fact]
    public void FromJsonElement_LongNumber_ReturnsLong() => Assert.Equal(long.MaxValue, TypeConversion.FromJsonElement(ToElement(long.MaxValue)));

    [Fact]
    public void FromJsonElement_DoubleNumber_ReturnsDouble() => Assert.Equal(1.5, TypeConversion.FromJsonElement(ToElement(1.5)));

    [Fact]
    public void FromJsonElement_Booleans_ReturnBool()
    {
        Assert.Equal(true, TypeConversion.FromJsonElement(ToElement(true)));
        Assert.Equal(false, TypeConversion.FromJsonElement(ToElement(false)));
    }

    [Fact]
    public void FromJsonElement_Null_ReturnsNull() => Assert.Null(TypeConversion.FromJsonElement(JsonSerializer.SerializeToElement<string?>(null)));

    [Fact]
    public void FromJsonElement_Array_ReturnsRecursiveList()
    {
        var result = TypeConversion.FromJsonElement(ToElement(new object[] { "a", 1, true }));
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(new object?[] { "a", 1, true }, list);
    }

    [Fact]
    public void FromJsonElement_Object_ReturnsRawText()
    {
        var result = TypeConversion.FromJsonElement(JsonSerializer.SerializeToElement(new { a = 1 }));
        Assert.Equal("{\"a\":1}", result);
    }

    // ---- FromJsonElement (typed) / TryFromJsonElement ----

    [Fact]
    public void FromJsonElement_Typed_ConvertsNumber() => Assert.Equal(42L, TypeConversion.FromJsonElement(ToElement(42), typeof(long)));

    [Fact]
    public void FromJsonElement_Typed_WrongKind_Throws() => Assert.ThrowsAny<Exception>(() => TypeConversion.FromJsonElement(ToElement("abc"), typeof(int)));

    [Fact]
    public void TryFromJsonElement_ValidNumber_ReturnsTrue()
    {
        Assert.True(TypeConversion.TryFromJsonElement<int>(ToElement(42), out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryFromJsonElement_WrongKind_ReturnsFalse() => Assert.False(TypeConversion.TryFromJsonElement<int>(ToElement("abc"), out var _));

    [Fact]
    public void TryFromJsonElement_DateTimeString_Parses()
    {
        Assert.True(TypeConversion.TryFromJsonElement<DateTime>(ToElement("2026-07-26T10:00:00Z"), out var value));
        Assert.Equal(new(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc), value.ToUniversalTime());
    }

    // ---- ConvertToWithCollections ----

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToStringList()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { "a", "b" }), typeof(List<string>));
        Assert.Equal(["a", "b"], Assert.IsType<List<string>>(result));
    }

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToIntArray()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { 1, 2, 3 }), typeof(int[]));
        Assert.Equal([1, 2, 3], Assert.IsType<int[]>(result));
    }

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToHashSet()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { "a", "b", "a" }), typeof(HashSet<string>));
        Assert.Equal(2, Assert.IsType<HashSet<string>>(result).Count);
    }

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToIReadOnlyList()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { "a", "b" }), typeof(IReadOnlyList<string>));
        Assert.Equal(["a", "b"], Assert.IsAssignableFrom<IReadOnlyList<string>>(result));
    }

    [Fact]
    public void ConvertToWithCollections_SingleValue_WrapsInList()
    {
        var result = TypeConversion.ConvertToWithCollections("a", typeof(List<string>));
        Assert.Equal(["a"], Assert.IsType<List<string>>(result));
    }

    [Fact]
    public void ConvertToWithCollections_EnumerableElements_ConvertsEachElement()
    {
        var result = TypeConversion.ConvertToWithCollections(new object[] { "1", 2, 3L }, typeof(List<int>));
        Assert.Equal([1, 2, 3], Assert.IsType<List<int>>(result));
    }

    [Fact]
    public void ConvertToWithCollections_ScalarTarget_ConvertsDirectly() => Assert.Equal(42, TypeConversion.ConvertToWithCollections("42", typeof(int)));

    [Fact]
    public void ConvertToWithCollections_NullToNonNullableValueType_Throws()
        => Assert.Throws<ArgumentNullException>(() => TypeConversion.ConvertToWithCollections(null, typeof(int)));

    [Fact]
    public void ConvertToWithCollections_NullToReferenceType_ReturnsNull() => Assert.Null(TypeConversion.ConvertToWithCollections(null, typeof(string)));

    // ---- EnumOrDefault / EnumOrNull ----

    [Theory]
    [InlineData("First", SampleEnum.First)]
    [InlineData("second", SampleEnum.Second)]
    [InlineData("2", SampleEnum.Second)]
    [InlineData("bogus", SampleEnum.Unknown)]
    [InlineData(null, SampleEnum.Unknown)]
    public void EnumOrDefault_ParsesOrFallsBack(string? input, SampleEnum expected) => Assert.Equal(expected, TypeConversion.EnumOrDefault(input, SampleEnum.Unknown));

    [Fact]
    public void EnumOrDefault_CustomDefault_ReturnedOnFailure() => Assert.Equal(SampleEnum.Second, TypeConversion.EnumOrDefault("bogus", SampleEnum.Second));

    [Theory]
    [InlineData("First", SampleEnum.First)]
    [InlineData("bogus", null)]
    [InlineData(null, null)]
    [InlineData(" ", null)]
    public void EnumOrNull_ParsesOrReturnsNull(string? input, SampleEnum? expected) => Assert.Equal(expected, TypeConversion.EnumOrNull<SampleEnum>(input));

    // ---- ToBoolean ----

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("on", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("off", false)]
    public void ToBoolean_ParsesLenientTokens(string input, bool expected) => Assert.Equal(expected, TypeConversion.ToBoolean(input));

    [Fact]
    public void ToBoolean_InvalidToken_Throws() => Assert.Throws<TypeConversionException>(() => TypeConversion.ToBoolean("maybe"));

    // ---- GetUnderlyingType / IsObjectEnumerable ----

    [Fact]
    public void GetUnderlyingType_Nullable_ReturnsUnderlying() => Assert.Equal(typeof(int), TypeConversion.GetUnderlyingType(typeof(int?)));

    [Fact]
    public void GetUnderlyingType_NonNullable_ReturnsSameType() => Assert.Equal(typeof(string), TypeConversion.GetUnderlyingType(typeof(string)));

    [Fact]
    public void IsObjectEnumerable_ExcludesStringAndByteArray()
    {
        Assert.True(TypeConversion.IsObjectEnumerable(new List<int>()));
        Assert.True(TypeConversion.IsObjectEnumerable(new[] { 1 }));
        Assert.False(TypeConversion.IsObjectEnumerable("abc"));
        Assert.False(TypeConversion.IsObjectEnumerable(new byte[] { 1 }));
        Assert.False(TypeConversion.IsObjectEnumerable(null));
    }

    // ---- Type extension helpers ----

    [Fact]
    public void TypeExtensions_ClassifyTypes()
    {
        Assert.True(typeof(int).IsNumericType());
        Assert.False(typeof(string).IsNumericType());
        Assert.True(typeof(int?).IsNullable());
        Assert.False(typeof(int).IsNullable());
        Assert.True(typeof(List<int>).IsCollectionType());
        Assert.True(typeof(int[]).IsCollectionType());
        Assert.False(typeof(string).IsCollectionType());
        Assert.Equal(typeof(int), typeof(List<int>).GetCollectionElementType());
        Assert.Equal(typeof(string), typeof(string[]).GetCollectionElementType());
        Assert.Equal("List<Int32>", typeof(List<int>).GetFriendlyTypeName());
    }

    // ---- TypeConversionException ----

    [Fact]
    public void TypeConversionException_CarriesConversionContext()
    {
        var ex = Assert.Throws<TypeConversionException>(() => TypeConversion.ConvertTo("not-a-guid", typeof(Guid)));
        Assert.Equal("not-a-guid", ex.Value);
        Assert.Equal(typeof(string), ex.SourceType);
        Assert.Equal(typeof(Guid), ex.TargetType);
    }

    [Fact]
    public void TypeConversionException_IsCatchableAsInvalidOperationException()
    {
        var ex = Assert.ThrowsAny<InvalidOperationException>(() => TypeConversion.ConvertTo("nope", typeof(SampleEnum)));
        Assert.IsType<TypeConversionException>(ex);
    }

    // ---- TryConvertTo ----

    [Fact]
    public void TryConvertTo_Generic_Success()
    {
        Assert.True(TypeConversion.TryConvertTo<int>("42", out var result));
        Assert.Equal(42, result);
    }

    [Fact]
    public void TryConvertTo_Generic_Failure_DoesNotThrow()
    {
        Assert.False(TypeConversion.TryConvertTo<int>("abc", out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryConvertTo_Null_SucceedsWithNullResult()
    {
        Assert.True(TypeConversion.TryConvertTo<string>(null, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void TryConvertTo_NonGeneric_Success()
    {
        Assert.True(TypeConversion.TryConvertTo("42", typeof(long), out var result));
        Assert.Equal(42L, result);
    }

    [Fact]
    public void TryConvertTo_NonGeneric_Failure()
    {
        Assert.False(TypeConversion.TryConvertTo(new(), typeof(int), out var result));
        Assert.Null(result);
    }

    [Fact]
    public void TryConvertTo_Enum_MissDoesNotThrow() => Assert.False(TypeConversion.TryConvertTo<SampleEnum>("bogus", out var _));

    [Fact]
    public void TryConvertTo_LenientBoolean_ParsesTokens()
    {
        Assert.True(TypeConversion.TryConvertTo<bool>("yes", out var result, true));
        Assert.True(result);
    }

    // ---- TryConvertToWithCollections ----

    [Fact]
    public void TryConvertToWithCollections_Success()
    {
        Assert.True(TypeConversion.TryConvertToWithCollections(ToElement(new[] { 1, 2 }), typeof(List<int>), out var result));
        Assert.Equal([1, 2], Assert.IsType<List<int>>(result));
    }

    [Fact]
    public void TryConvertToWithCollections_ElementFailure_ReturnsFalse()
    {
        Assert.False(TypeConversion.TryConvertToWithCollections(new object[] { "abc" }, typeof(List<int>), out var result));
        Assert.Null(result);
    }

    // ---- Non-generic TryFromJsonElement ----

    [Fact]
    public void TryFromJsonElement_NonGeneric_Success()
    {
        Assert.True(TypeConversion.TryFromJsonElement(ToElement(42), typeof(int), out var result));
        Assert.Equal(42, result);
    }

    [Fact]
    public void TryFromJsonElement_NonGeneric_WrongKind_ReturnsFalse() => Assert.False(TypeConversion.TryFromJsonElement(ToElement("abc"), typeof(int), out var _));

    // ---- ConvertToOrDefault ----

    [Fact]
    public void ConvertToOrDefault_Success_ReturnsConverted() => Assert.Equal(42, TypeConversion.ConvertToOrDefault("42", -1));

    [Fact]
    public void ConvertToOrDefault_Failure_ReturnsDefault() => Assert.Equal(-1, TypeConversion.ConvertToOrDefault("abc", -1));

    [Fact]
    public void ConvertToOrDefault_Null_ReturnsDefault() => Assert.Equal(-1, TypeConversion.ConvertToOrDefault(null, -1));

    // ---- ToEnumerable / ConvertToArray / ConvertToList ----

    [Fact]
    public void ToEnumerable_Null_ReturnsEmpty() => Assert.Empty(TypeConversion.ToEnumerable(null));

    [Fact]
    public void ToEnumerable_Scalar_WrapsValue() => Assert.Equal([42], TypeConversion.ToEnumerable(42));

    [Fact]
    public void ToEnumerable_String_TreatedAsScalar() => Assert.Equal(["abc"], TypeConversion.ToEnumerable("abc"));

    [Fact]
    public void ToEnumerable_Enumerable_Enumerates() => Assert.Equal([1, 2, 3], TypeConversion.ToEnumerable(new[] { 1, 2, 3 }));

    [Fact]
    public void ToEnumerable_JsonArray_ExtractsElements() => Assert.Equal(new object?[] { "a", 1, true }, TypeConversion.ToEnumerable(ToElement(new object[] { "a", 1, true })));

    [Fact]
    public void ToEnumerable_JsonNull_ReturnsEmpty() => Assert.Empty(TypeConversion.ToEnumerable(JsonSerializer.SerializeToElement<string?>(null)));

    [Fact]
    public void ToEnumerable_JsonScalar_WrapsValue() => Assert.Equal([42], TypeConversion.ToEnumerable(ToElement(42)));

    [Fact]
    public void ConvertToArray_ConvertsEachElement() => Assert.Equal([1, 2, 3], TypeConversion.ConvertToArray<int>(["1", 2, 3L]));

    [Fact]
    public void ConvertToArray_Null_ReturnsEmpty() => Assert.Empty(TypeConversion.ConvertToArray<int>(null));

    [Fact]
    public void ConvertToList_ConvertsEachElement() => Assert.Equal([1, 2], TypeConversion.ConvertToList<int>(["1", "2"]));

    [Fact]
    public void ConvertToList_Null_ReturnsEmpty() => Assert.Empty(TypeConversion.ConvertToList<int>(null));

    // ---- Boolean token sets ----

    [Fact]
    public void DefaultTokenSets_ContainExpectedTokens()
    {
        Assert.Equal(["true", "t", "1", "y", "yes", "on"], TypeConversion.DefaultTrueValues);
        Assert.Equal(["false", "f", "0", "n", "no", "off"], TypeConversion.DefaultFalseValues);
    }

    [Fact]
    public void TryToBoolean_DefaultTokens_ParsesCaseInsensitively()
    {
        Assert.True(TypeConversion.TryToBoolean("YES", out var result));
        Assert.True(result);
        Assert.True(TypeConversion.TryToBoolean("Off", out result));
        Assert.False(result);
    }

    [Fact]
    public void TryToBoolean_NullOrUnknown_ReturnsFalse()
    {
        Assert.False(TypeConversion.TryToBoolean(null, out var _));
        Assert.False(TypeConversion.TryToBoolean("maybe", out var _));
    }

    [Fact]
    public void ToBoolean_CustomTokenSets_OverrideDefaults()
    {
        Assert.True(TypeConversion.ToBoolean("enabled", ["enabled"], ["disabled"]));
        Assert.False(TypeConversion.ToBoolean("DISABLED", ["enabled"], ["disabled"]));
    }

    [Fact]
    public void TryToBoolean_CustomTokens_UnknownToken_FallsBackToBoolParse()
    {
        // "yes" is not in the custom sets and is not bool-parseable, so it misses
        Assert.False(TypeConversion.TryToBoolean("yes", out var _, ["enabled"], ["disabled"]));
        // "true" still parses via bool.TryParse even with custom sets
        Assert.True(TypeConversion.TryToBoolean("true", out var result, ["enabled"], ["disabled"]));
        Assert.True(result);
    }

    // ---- Span overloads ----

    [Fact]
    public void ConvertTo_Span_ParsesInt() => Assert.Equal(42, TypeConversion.ConvertTo<int>("42".AsSpan()));

    [Fact]
    public void ConvertTo_Span_ParsesGuid()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, TypeConversion.ConvertTo<Guid>(guid.ToString().AsSpan()));
    }

    [Fact]
    public void ConvertTo_Span_ParsesEnum() => Assert.Equal(SampleEnum.Second, TypeConversion.ConvertTo<SampleEnum>("second".AsSpan()));

    [Fact]
    public void ConvertTo_Span_ParsesNullableTarget() => Assert.Equal(42, TypeConversion.ConvertTo<int?>("42".AsSpan()));

    [Fact]
    public void ConvertTo_Span_Invalid_Throws()
    {
        static void Act() => TypeConversion.ConvertTo<int>("abc".AsSpan());

        Assert.Throws<TypeConversionException>(Act);
    }

    [Fact]
    public void TryConvertTo_Span_Success()
    {
        Assert.True(TypeConversion.TryConvertTo<double>("42.5".AsSpan(), out var result));
        Assert.Equal(42.5, result);
    }

    [Fact]
    public void TryConvertTo_Span_Failure()
    {
        Assert.False(TypeConversion.TryConvertTo<int>("abc".AsSpan(), out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void ToBoolean_Span_ParsesLenientTokens()
    {
        Assert.True(TypeConversion.ToBoolean("yes".AsSpan()));
        Assert.False(TypeConversion.ToBoolean("OFF".AsSpan()));
    }

    [Fact]
    public void TryToBoolean_Span_UnknownToken_ReturnsFalse() => Assert.False(TypeConversion.TryToBoolean("maybe".AsSpan(), out var _));

    // ---- Collection-target materialization ----

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToIReadOnlyCollection()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { 1, 2 }), typeof(IReadOnlyCollection<int>));
        Assert.Equal([1, 2], Assert.IsAssignableFrom<IReadOnlyCollection<int>>(result));
    }

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToISet()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { "a", "b", "a" }), typeof(ISet<string>));
        Assert.Equal(2, Assert.IsAssignableFrom<ISet<string>>(result).Count);
    }

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToQueue()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { 1, 2 }), typeof(Queue<int>));
        Assert.Equal([1, 2], Assert.IsType<Queue<int>>(result));
    }

    [Fact]
    public void ConvertToWithCollections_JsonArray_ToIEnumerable()
    {
        var result = TypeConversion.ConvertToWithCollections(ToElement(new[] { 1, 2 }), typeof(IEnumerable<int>));
        Assert.Equal([1, 2], Assert.IsAssignableFrom<IEnumerable<int>>(result));
    }

    [Fact]
    public void ConvertTo_JsonObjectString_DeserializesComplexType()
    {
        var result = TypeConversion.ConvertTo<SamplePayload>("""{"Name":"abc","Count":3}""");
        Assert.Equal(new("abc", 3), result);
    }

    [Fact]
    public void ConvertTo_JsonArrayString_DeserializesListTarget()
    {
        var result = TypeConversion.ConvertTo<List<int>>("[1,2,3]");
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void ConvertTo_JsonStringWithLeadingWhitespace_Deserializes()
    {
        var result = TypeConversion.ConvertTo<SamplePayload>("""  {"Name":"x","Count":1}""");
        Assert.Equal(new("x", 1), result);
    }

    [Fact]
    public void TryConvertTo_NonJsonString_ToComplexType_Fails() => Assert.False(TypeConversion.TryConvertTo<SamplePayload>("not json", out var _));

    [Fact]
    public void TryConvertTo_MalformedJson_ToComplexType_Fails() => Assert.False(TypeConversion.TryConvertTo<SamplePayload>("{broken", out var _));

    [Fact]
    public void ConvertTo_JsonLookingString_ToScalarTarget_StillFails() => Assert.Throws<TypeConversionException>(() => TypeConversion.ConvertTo<int>("[1]"));

    // ---- Logger ----

    [Fact]
    public void Logger_TryMiss_LogsWarning_AndThrow_LogsError()
    {
        var logger = new CapturingLogger();
        var original = TypeConversion.Logger;
        TypeConversion.Logger = logger;
        try {
            Assert.False(TypeConversion.TryConvertTo<int>("abc", out var _));
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("abc"));
            Assert.Throws<TypeConversionException>(() => TypeConversion.ConvertTo("not-a-guid", typeof(Guid)));
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("not-a-guid"));
        }
        finally {
            TypeConversion.Logger = original;
        }
    }

    [Fact]
    public void Logger_SuccessfulConversion_LogsDebug()
    {
        var logger = new CapturingLogger();
        var original = TypeConversion.Logger;
        TypeConversion.Logger = logger;
        try {
            TypeConversion.ConvertTo("42", typeof(int));
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("Int32"));
        }
        finally {
            TypeConversion.Logger = original;
        }
    }

    // ---- JSON-string fallback (complex targets) ----

    public sealed record SamplePayload(string Name, int Count);

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}