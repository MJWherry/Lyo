using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Json.Tests;

public class JsonOptionsTests
{
    [Fact]
    public void ExportToJsonString_SnakeCase_WritesGivenName()
    {
        var json = new JsonService(o => o.Naming = DocumentNaming.SnakeCase).ExportToJsonString(new Person { GivenName = "Ada" });
        Assert.Contains("\"given_name\":\"Ada\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportToJsonString_EnumNumber_WritesZero()
    {
        var json = new JsonService(o => o.Enums = DocumentEnumFormat.Number).ExportToJsonString(new Person { GivenName = "Ada", Role = Role.Admin });
        Assert.Contains("\"role\":0", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseString_LeadingZero_StaysString()
    {
        var node = new JsonService(o => o.ReadNumbersFromStrings = true).ParseString<JsonObject>("""{"code":"01"}""");
        Assert.Equal("01", node["code"]!.GetValue<string>());
    }

    [Fact]
    public void ParseString_QuotedAge_BindsIntOnPoco()
    {
        var plain = new JsonService().ParseString<Person>("""{"age":"36"}""");
        var coerced = new JsonService(o => o.ReadNumbersFromStrings = true).ParseString<Person>("""{"age":"36"}""");
        Assert.Equal(36, plain.Age);
        Assert.Equal(36, coerced.Age);
    }

    [Fact]
    public void ParseString_ReadNumbersFromStrings_MakesJsonNodeNumber()
    {
        var node = new JsonService(o => o.ReadNumbersFromStrings = true).ParseString<JsonObject>("""{"age":"36"}""");
        Assert.Equal(36, node["age"]!.GetValue<int>());
    }

    [Fact]
    public void ExportToJsonString_WriteNumbersAsStrings_QuotesAge()
    {
        var json = new JsonService(o => o.WriteNumbersAsStrings = true).ExportToJsonString(new Person { Age = 36 });
        Assert.Contains("\"age\":\"36\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void UseStringScalarConverters_RoundTripsQuotedDecimal()
    {
        var svc = new JsonService(o => o.UseStringScalarConverters = true);
        var back = svc.ParseString<Person>("""{"amount":"12"}""");
        Assert.Equal(12m, back.Amount);
        var json = svc.ExportToJsonString(new Person { Amount = 12m });
        Assert.Contains("\"amount\":\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportToJsonString_IgnoreNullsFalse_WritesNull()
    {
        var json = new JsonService(o => o.IgnoreNulls = false).ExportToJsonString(new Person { GivenName = "Ada", Note = null });
        Assert.Contains("\"note\":null", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportToJsonString_IgnoreDefaults_OmitsZeroAndFalse()
    {
        var json = new JsonService(o => o.IgnoreDefaults = true).ExportToJsonString(new Person { GivenName = "Ada", Years = 0, Active = false });
        Assert.Contains("\"givenName\":\"Ada\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("years", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("active", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportToJsonString_IgnoreEmptyStrings_OmitsEmpty()
    {
        var json = new JsonService(o => o.IgnoreEmptyStrings = true).ExportToJsonString(new Person { GivenName = "Ada", Note = "" });
        Assert.DoesNotContain("note", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportToJsonString_IgnoreEmptyCollections_OmitsEmptyArray()
    {
        var json = new JsonService(o => o.IgnoreEmptyCollections = true).ExportToJsonString(new Person { GivenName = "Ada", Tags = [] });
        Assert.DoesNotContain("tags", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseString_EmptyStringAsNull_BindsNull()
    {
        var person = new JsonService(o => o.EmptyStringAsNull = true).ParseString<Person>("""{"note":""}""");
        Assert.Null(person.Note);
    }

    [Fact]
    public void ParseString_UnmappedMember_Throws()
    {
        var svc = new JsonService(o => o.UnmappedMembers = DocumentUnmappedMembers.Error);
        Assert.ThrowsAny<Exception>(() => svc.ParseString<Person>("""{"givenName":"Ada","extra":1}"""));
    }

    [Fact]
    public void ParseString_CommentHandlingSkip_AcceptsComment()
    {
        var person = new JsonService(o => o.CommentHandling = DocumentCommentHandling.Skip).ParseString<Person>("""{ "givenName": "Ada" /* hi */ }""");
        Assert.Equal("Ada", person.GivenName);
    }

    [Fact]
    public void ParseString_TrailingComma_RequiresTheFlag()
    {
        const string json = """{ "givenName": "Ada", }""";
        Assert.ThrowsAny<Exception>(() => new JsonService().ParseString<Person>(json));
        var person = new JsonService(o => o.AllowTrailingCommas = true).ParseString<Person>(json);
        Assert.Equal("Ada", person.GivenName);
    }

    [Fact]
    public void ExportToJsonString_UnixMilliseconds_RoundTripsDateTimeOffset()
    {
        var when = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);
        var svc = new JsonService(o => o.DateTimeStyle = DocumentDateTimeStyle.UnixMilliseconds);
        var json = svc.ExportToJsonString(new Person { When = when });
        Assert.Contains("1700000000000", json, StringComparison.Ordinal);
        var back = svc.ParseString<Person>(json);
        Assert.Equal(when, back.When);
    }

    [Fact]
    public void ParseFile_Windows1252_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7() + ".json");
        try {
            var encoding = Encoding.GetEncoding(1252);
            var svc = new JsonService(o => o.Encoding = encoding);
            svc.ExportToJson(new Person { GivenName = "Jos\u00e9" }, path);
            var person = svc.ParseFile<Person>(path);
            Assert.Equal("Jos\u00e9", person.GivenName);
            Assert.Equal(encoding.GetString(encoding.GetBytes("Jos\u00e9")), person.GivenName);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ParseFile_Utf8Bom_ParsesWhenEncodingIsUtf8()
    {
        var path = Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7() + ".json");
        try {
            var body = Encoding.UTF8.GetBytes("""{"givenName":"Ada"}""");
            var bom = Encoding.UTF8.GetPreamble();
            File.WriteAllBytes(path, [.. bom, .. body]);
            var person = new JsonService().ParseFile<Person>(path);
            Assert.Equal("Ada", person.GivenName);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Parse_Utf8Span_BindsPerson()
    {
        var bytes = Encoding.UTF8.GetBytes("""{"givenName":"Ada"}""");
        var person = JsonService.Default.Parse<Person>(bytes.AsSpan());
        Assert.Equal("Ada", person.GivenName);
    }

    [Fact]
    public void Parse_Utf16Span_UsesEncodingArgument()
    {
        var bytes = Encoding.Unicode.GetBytes("""{"givenName":"Ada"}""");
        var person = JsonService.Default.Parse<Person>(bytes.AsSpan(), Encoding.Unicode);
        Assert.Equal("Ada", person.GivenName);
    }

    [Fact]
    public void TryExport_OneByteBuffer_ReturnsFalse()
    {
        var ok = JsonService.Default.TryExport(new Person { GivenName = "Ada" }, new byte[1], out var written);
        Assert.False(ok);
        Assert.Equal(0, written);
    }

    [Fact]
    public void ParseNdjson_TwoPeople_ReturnsBoth()
    {
        var people = JsonService.Default.ParseNdjsonString<Person>("""{"givenName":"Ada"}""" + "\n\n" + """{"givenName":"Grace"}""" + "\n");
        Assert.Equal(2, people.Count);
        Assert.Equal("Ada", people[0].GivenName);
        Assert.Equal("Grace", people[1].GivenName);
    }

    [Fact]
    public async Task ParseNdjsonStreamAsync_TwoPeople_YieldsBoth()
    {
        var text = """{"givenName":"Ada"}""" + "\n" + """{"givenName":"Grace"}""" + "\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var names = new List<string?>();
        await foreach (var person in JsonService.Default.ParseNdjsonStreamAsync<Person>(stream, TestCt))
            names.Add(person.GivenName);

        Assert.Equal(["Ada", "Grace"], names);
    }

    [Fact]
    public async Task Default_ParallelParseAndExport_ReturnsTheDocument()
    {
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => {
                var json = JsonService.Default.ExportToJsonString(new Person { GivenName = "Ada", Age = 36 });
                var person = JsonService.Default.ParseString<Person>(json);
                Assert.Equal("Ada", person.GivenName);
                Assert.Equal(36, person.Age);
            }));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public void Constructor_JsonSerializerOptions_WritesPascalCase()
    {
        var host = new JsonSerializerOptions { PropertyNamingPolicy = null, WriteIndented = false };
        var json = new JsonService(host).ExportToJsonString(new Person { GivenName = "Ada" });
        Assert.Contains("\"GivenName\":\"Ada\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_JsonOptions_WritesSnakeCase()
    {
        var json = new JsonService(new JsonOptions { Naming = DocumentNaming.SnakeCase }).ExportToJsonString(new Person { GivenName = "Ada" });
        Assert.Contains("\"given_name\":\"Ada\"", json, StringComparison.Ordinal);
    }

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public int Years { get; set; }

        public bool Active { get; set; }

        public string? Note { get; set; }

        public string? Code { get; set; }

        public List<string>? Tags { get; set; }

        public Role Role { get; set; }

        public decimal Amount { get; set; }

        public DateTimeOffset When { get; set; }
    }

    private enum Role
    {
        Admin = 0
    }
}