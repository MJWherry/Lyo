using System.Text.Json.Nodes;
using System.Xml.Linq;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Tests;

public class LyoTypeInfoTests
{
    public enum SampleEnum
    {
        Unknown = 0,
        First = 1
    }

    [Fact]
    public void StaticRegistry_ContainsExpectedMetadata()
    {
        Assert.Equal("int", LyoTypeInfo.Int.ShortName);
        Assert.Equal(typeof(int).FullName, LyoTypeInfo.Int.FullName);
        Assert.Equal(typeof(int), LyoTypeInfo.Int.Type);
        Assert.Equal(LyoTypeCategory.Integer, LyoTypeInfo.Int.Category);
        Assert.Equal(LyoTypeEditorKind.Integer, LyoTypeInfo.Int.EditorKind);
        Assert.True(LyoTypeInfo.Int.IsScalar);
        Assert.Equal("0", LyoTypeInfo.Int.DefaultJson);
        Assert.Null(LyoTypeInfo.Int.ElementType);

        Assert.False(LyoTypeInfo.JsonObject.IsScalar);
        Assert.Equal(LyoTypeEditorKind.JsonObject, LyoTypeInfo.JsonObject.EditorKind);
        Assert.Equal("{}", LyoTypeInfo.JsonObject.DefaultJson);

        Assert.True(LyoTypeInfo.StringList.IsCollection);
        Assert.Equal(typeof(string), LyoTypeInfo.StringList.ElementType);
        Assert.Equal("[]", LyoTypeInfo.StringList.DefaultJson);
        Assert.Equal(LyoTypeEditorKind.Collection, LyoTypeInfo.StringList.EditorKind);

        Assert.Equal(typeof(Enum), LyoTypeInfo.Enum.Type);
        Assert.Equal(LyoTypeEditorKind.Enum, LyoTypeInfo.Enum.EditorKind);
        Assert.Equal("0", LyoTypeInfo.Enum.DefaultJson);
        Assert.Equal("\"00:00:00\"", LyoTypeInfo.TimeSpan.DefaultJson);
        Assert.Equal(LyoTypeEditorKind.Duration, LyoTypeInfo.TimeSpan.EditorKind);
        Assert.Equal(LyoTypeEditorKind.Regex, LyoTypeInfo.Regex.EditorKind);
        Assert.Equal(typeof(System.Text.RegularExpressions.Regex), LyoTypeInfo.Regex.Type);
        Assert.Equal(LyoTypeEditorKind.Xml, LyoTypeInfo.Xml.EditorKind);
        Assert.Equal(typeof(XDocument), LyoTypeInfo.Xml.Type);
        Assert.True(LyoTypeInfo.Int.IsClr);
    }

    [Fact]
    public void All_ExcludesUnknown_AndIncludesScalarsJsonAndCollections()
    {
        Assert.DoesNotContain(LyoTypeInfo.Unknown, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.String, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.Bool, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.Int, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.TimeSpan, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.Enum, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.Regex, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.Xml, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.JsonArray, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.StringList, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.IntArray, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.ByteArray, LyoTypeInfo.All);
        Assert.Contains(LyoTypeInfo.All, t => t.Type == typeof(string) && t.IsScalar);
        Assert.Contains(LyoTypeInfo.All, t => t.Type == typeof(JsonObject) && !t.IsScalar);
        Assert.Contains(LyoTypeInfo.All, t => t.Type == typeof(JsonArray) && !t.IsScalar);
    }

    [Fact]
    public void ByCategory_ReturnsMatchingTypes()
    {
        var integers = LyoTypeInfo.ByCategory(LyoTypeCategory.Integer).ToList();
        Assert.Contains(LyoTypeInfo.Int, integers);
        Assert.Contains(LyoTypeInfo.Long, integers);
        Assert.DoesNotContain(LyoTypeInfo.Decimal, integers);

        var collections = LyoTypeInfo.ByCategory(LyoTypeCategory.Collection).ToList();
        Assert.Contains(LyoTypeInfo.StringList, collections);
        Assert.Contains(LyoTypeInfo.IntArray, collections);
        Assert.DoesNotContain(LyoTypeInfo.JsonArray, collections);

        var text = LyoTypeInfo.ByCategory(LyoTypeCategory.Text).ToList();
        Assert.Contains(LyoTypeInfo.String, text);
        Assert.Contains(LyoTypeInfo.Uri, text);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("Int32")]
    [InlineData("int32")]
    public void FromName_ResolvesIntAliases(string name) => Assert.Equal(LyoTypeInfo.Int, LyoTypeInfo.FromName(name));

    [Fact]
    public void FromName_MatchesFullNameOrdinal()
    {
        var found = LyoTypeInfo.FromName(typeof(int).FullName);
        Assert.Equal(LyoTypeInfo.Int, found);
        Assert.Same(LyoTypeInfo.Int, LyoTypeInfo.FromName("int"));
        Assert.Equal(LyoTypeInfo.Unknown, LyoTypeInfo.FromName("Not.A.Real.Type"));
        Assert.Equal(LyoTypeInfo.Unknown, LyoTypeInfo.FromName(null));
        Assert.Equal(LyoTypeInfo.Unknown, LyoTypeInfo.FromName("  "));
    }

    [Fact]
    public void FromName_SampleEnumFullName_ReturnsEnum()
    {
        var fullName = typeof(SampleEnum).FullName;
        Assert.False(string.IsNullOrEmpty(fullName));
        Assert.Equal(LyoTypeInfo.Enum, LyoTypeInfo.FromName(fullName));
        Assert.Equal(typeof(SampleEnum), LyoTypeInfo.TryResolveClrType(fullName));
    }

    [Fact]
    public void TryResolveClrType_UnknownName_ReturnsNull() => Assert.Null(LyoTypeInfo.TryResolveClrType("Not.A.Real.Type"));

    [Theory]
    [InlineData("enum")]
    [InlineData("Enum")]
    [InlineData("System.Enum")]
    public void FromName_ResolvesEnum(string name) => Assert.Equal(LyoTypeInfo.Enum, LyoTypeInfo.FromName(name));

    [Theory]
    [InlineData("JSON object")]
    [InlineData("json object")]
    [InlineData("JsonObject")]
    public void FromName_ResolvesJsonObject(string name) => Assert.Equal(LyoTypeInfo.JsonObject, LyoTypeInfo.FromName(name));

    [Fact]
    public void FromName_ResolvesCollectionAliases()
    {
        Assert.Equal(LyoTypeInfo.StringList, LyoTypeInfo.FromName("List<string>"));
        Assert.Equal(LyoTypeInfo.StringList, LyoTypeInfo.FromName("string list"));
        Assert.Equal(LyoTypeInfo.StringArray, LyoTypeInfo.FromName("string[]"));
        Assert.Equal(LyoTypeInfo.IntArray, LyoTypeInfo.FromName("int array"));
    }

    [Fact]
    public void TryFromName_Known_ReturnsTrue()
    {
        Assert.True(LyoTypeInfo.TryFromName("bool", out var info));
        Assert.Equal(LyoTypeInfo.Bool, info);
    }

    [Fact]
    public void TryFromName_Unknown_ReturnsFalse() => Assert.False(LyoTypeInfo.TryFromName("Not.A.Real.Type", out var _));

    [Fact]
    public void FromType_ExactMatch()
    {
        Assert.Equal(LyoTypeInfo.String, LyoTypeInfo.FromType(typeof(string)));
        Assert.Equal(LyoTypeInfo.Int, LyoTypeInfo.FromType(typeof(int)));
        Assert.Equal(LyoTypeInfo.Int, LyoTypeInfo.FromType(typeof(int?)));
        Assert.Equal(LyoTypeInfo.StringList, LyoTypeInfo.FromType(typeof(List<string>)));
        Assert.Equal(LyoTypeInfo.JsonArray, LyoTypeInfo.FromType(typeof(JsonArray)));
        Assert.Equal(LyoTypeInfo.Unknown, LyoTypeInfo.FromType(typeof(uint)));
        Assert.Equal(LyoTypeInfo.Unknown, LyoTypeInfo.FromType(null));
    }

    [Fact]
    public void FromType_SampleEnum_ReturnsEnum()
    {
        Assert.Equal(LyoTypeInfo.Enum, LyoTypeInfo.FromType(typeof(SampleEnum)));
        Assert.Equal(LyoTypeInfo.Enum, LyoTypeInfo.FromType(typeof(SampleEnum?)));
        Assert.Equal(LyoTypeInfo.Enum, LyoTypeInfo.FromType(typeof(Enum)));
    }

    [Fact]
    public void TryFromType_Known_ReturnsTrue()
    {
        Assert.True(LyoTypeInfo.TryFromType(typeof(Guid), out var info));
        Assert.Equal(LyoTypeInfo.Guid, info);
    }

    [Fact]
    public void TryFromType_Unknown_ReturnsFalse() => Assert.False(LyoTypeInfo.TryFromType(typeof(uint), out var _));

    [Fact]
    public void ImplicitType_ReturnsRuntimeType()
    {
        Type type = LyoTypeInfo.Int;
        Assert.Equal(typeof(int), type);
    }

    [Fact]
    public void ToString_IncludesShortNameAndFullName() => Assert.Equal($"int ({typeof(int).FullName})", LyoTypeInfo.Int.ToString());

    [Theory]
    [InlineData("Int")]
    [InlineData("Bool")]
    [InlineData("Long")]
    [InlineData("Regex")]
    [InlineData("Xml")]
    public void FromName_ResolvesLegacyJobEnumNames(string name) => Assert.NotEqual(LyoTypeInfo.Unknown, LyoTypeInfo.FromName(name));

    [Fact]
    public void FromName_LegacyEnumNames_MapToExpectedEntries()
    {
        Assert.Equal(LyoTypeInfo.Int, LyoTypeInfo.FromName("Int"));
        Assert.Equal(LyoTypeInfo.Bool, LyoTypeInfo.FromName("Bool"));
        Assert.Equal(LyoTypeInfo.Long, LyoTypeInfo.FromName("Long"));
        Assert.Equal(LyoTypeInfo.Regex, LyoTypeInfo.FromName("Regex"));
        Assert.Equal(LyoTypeInfo.Xml, LyoTypeInfo.FromName("Xml"));
        Assert.Equal(LyoTypeInfo.JsonNode, LyoTypeInfo.FromName("Json"));
        Assert.Equal(LyoTypeInfo.String, LyoTypeInfo.FromName("String"));
    }

    [Fact]
    public void FromType_UnlistedList_SynthesizesCollection()
    {
        var info = LyoTypeInfo.FromType(typeof(List<decimal>));
        Assert.NotEqual(LyoTypeInfo.Unknown, info);
        Assert.True(info.IsCollection);
        Assert.Equal(typeof(decimal), info.ElementType);
        Assert.Equal(LyoTypeEditorKind.Collection, info.EditorKind);
        Assert.Equal("[]", info.DefaultJson);
        Assert.Equal(typeof(List<decimal>).FullName, info.FullName);
        Assert.DoesNotContain(info, LyoTypeInfo.All);
    }

    [Fact]
    public void ListOf_Int_ReturnsIntList() => Assert.Equal(LyoTypeInfo.IntList, LyoTypeInfo.ListOf(typeof(int)));

    [Fact]
    public void ArrayOf_Int_ReturnsIntArray() => Assert.Equal(LyoTypeInfo.IntArray, LyoTypeInfo.ArrayOf(typeof(int)));

    [Fact]
    public void NormalizeFullName_KnownAlias_ReturnsCatalogFullName()
        => Assert.Equal(typeof(int).FullName, LyoTypeInfo.NormalizeFullName("Int"));

    [Fact]
    public void Register_NonClr_DoesNotStealFromTypeString()
    {
        var registered = LyoTypeInfo.Register(
            "Demo",
            "Lyo.Tests.DemoType",
            "Test-only non-CLR catalog type.",
            LyoTypeCategory.Text,
            LyoTypeEditorKind.Text,
            "\"\"",
            typeof(string),
            isClr: false,
            aliases: ["demo-lyo-type"]);

        Assert.False(registered.IsClr);
        Assert.Same(registered, LyoTypeInfo.FromName("Lyo.Tests.DemoType"));
        Assert.Same(registered, LyoTypeInfo.FromName("demo-lyo-type"));
        Assert.Equal(LyoTypeInfo.String, LyoTypeInfo.FromType(typeof(string)));
        Assert.True(LyoTypeInfo.TryValidateJson("Lyo.Tests.DemoType", "\"ok\""));
        Assert.Equal("\"ok\"", registered.ToJson("ok"));
    }

    [Fact]
    public void TryValidateJson_Int_AcceptsNumber()
    {
        Assert.True(LyoTypeInfo.TryValidateJson("int", "42"));
        Assert.False(LyoTypeInfo.TryValidateJson("int", "\"nope\""));
        Assert.True(LyoTypeInfo.TryValidateJson("int", null));
    }

    [Fact]
    public void TryValidateJson_String_RequiresJsonString()
    {
        Assert.True(LyoTypeInfo.TryValidateJson("string", "\"hello\""));
        Assert.False(LyoTypeInfo.TryValidateJson("string", "hello"));
    }

    [Fact]
    public void TryValidateJson_RegexAndXml()
    {
        Assert.True(LyoTypeInfo.TryValidateJson("regex", "\"^[a-z]+$\""));
        Assert.False(LyoTypeInfo.TryValidateJson("regex", "\"[unclosed\""));
        Assert.True(LyoTypeInfo.TryValidateJson("xml", "\"<root/>\""));
        Assert.False(LyoTypeInfo.TryValidateJson("xml", "\"<root\""));
    }

    [Fact]
    public void TryValidateJson_Guid_RequiresParseableGuid()
    {
        Assert.True(LyoTypeInfo.TryValidateJson("Guid", "\"00000000-0000-0000-0000-000000000000\""));
        Assert.False(LyoTypeInfo.TryValidateJson("Guid", "\"s\""));
        Assert.True(LyoTypeInfo.TryValidateJson("Guid", null));
    }

    [Fact]
    public void TryValidateJson_Uri_RejectsBareToken()
    {
        Assert.True(LyoTypeInfo.TryValidateJson("Uri", "\"https://example.com/path\""));
        Assert.True(LyoTypeInfo.TryValidateJson("Uri", "\"/relative/path\""));
        Assert.False(LyoTypeInfo.TryValidateJson("Uri", "\"87gg\""));
        Assert.False(LyoTypeInfo.TryValidateJson("Uri", "\"s\""));
    }

    [Fact]
    public void IsValidUriText_AbsoluteAndPath_Accepted()
    {
        Assert.True(LyoTypeInfo.IsValidUriText("https://example.com"));
        Assert.True(LyoTypeInfo.IsValidUriText("/jobs/1"));
        Assert.False(LyoTypeInfo.IsValidUriText("87gg"));
        Assert.True(LyoTypeInfo.IsValidGuidText("00000000-0000-0000-0000-000000000000"));
        Assert.False(LyoTypeInfo.IsValidGuidText("s"));
    }

    [Fact]
    public void ToJson_SerializesScalars()
    {
        Assert.Equal("42", LyoTypeInfo.Int.ToJson(42));
        Assert.Equal("true", LyoTypeInfo.Bool.ToJson(true));
        Assert.Equal("\"hello\"", LyoTypeInfo.String.ToJson("hello"));
        Assert.Equal("[]", LyoTypeInfo.StringList.ToJson(new List<string>()));
    }
}
