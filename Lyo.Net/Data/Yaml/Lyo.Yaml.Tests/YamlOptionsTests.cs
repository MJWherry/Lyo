using System.Text;
using Lyo.Json;

namespace Lyo.Yaml.Tests;

public class YamlOptionsTests
{
    [Fact]
    public void ExportToYamlString_SnakeCase_WritesGivenNameKey()
    {
        var yaml = new YamlService(o => o.Naming = DocumentNaming.SnakeCase).ExportToYamlString(new Person { GivenName = "Ada" });
        Assert.Contains("given_name", yaml);
    }

    [Fact]
    public void ParseString_ReadNumbersFromStrings_BindsQuotedAndPlain()
    {
        var service = new YamlService(o => o.ReadNumbersFromStrings = true);
        var quoted = service.ParseString<Person>("age: \"36\"");
        var plain = service.ParseString<Person>("age: 36");
        Assert.Equal(36, quoted.Age);
        Assert.Equal(36, plain.Age);
    }

    [Fact]
    public void ConvertToJson_QuotedNumber_StaysStringUnlessCoerced()
    {
        var json = YamlService.Default.ConvertToJson("age: \"36\"");
        Assert.Contains("\"36\"", json);
        var coerced = new YamlService(o => o.ReadNumbersFromStrings = true).ConvertToJson("age: \"36\"");
        Assert.Contains("36", coerced);
        Assert.DoesNotContain("\"36\"", coerced);
    }

    [Fact]
    public void ExportToYamlString_FlowAndMarkers_FollowOptions()
    {
        var flow = new YamlService(o => o.WriteIndented = false).ExportToYamlString(new Person { GivenName = "Ada" });
        Assert.Contains("{", flow);
        Assert.Contains("givenName", flow);
        var marked = new YamlService(o => o.ExplicitMarkers = true).ExportToYamlString(new Person { GivenName = "Ada" });
        Assert.Contains("---", marked);
        Assert.Contains("...", marked);
    }

    [Fact]
    public void ParseString_MultiDocument_ReadsEachDocument()
    {
        const string yaml = """
                            ---
                            givenName: Ada
                            age: 36
                            ---
                            givenName: Grace
                            age: 37
                            """;

        var people = new YamlService(o => o.MultiDocument = true).ParseString<List<Person>>(yaml);
        Assert.Equal(2, people.Count);
        Assert.Equal("Grace", people[1].GivenName);
        var single = false;
        try {
            single = YamlService.Default.ParseString<List<Person>>(yaml).Count == 2;
        }
        catch {
            single = false;
        }

        Assert.False(single);
    }

    [Fact]
    public void ParseString_Alias_ThrowsUnlessAllowed()
    {
        const string yaml = """
                            givenName: &name Ada
                            note: *name
                            """;

        Assert.ThrowsAny<Exception>(() => YamlService.Default.ParseString<Person>(yaml));
        var person = new YamlService(o => o.AllowAliases = true).ParseString<Person>(yaml);
        Assert.Equal("Ada", person.Note);
    }

    [Fact]
    public void ExportToYamlString_DoubleQuoted_WrapsGivenName()
    {
        var yaml = new YamlService(o => o.QuoteStrings = YamlQuoteStyle.DoubleQuoted).ExportToYamlString(new Person { GivenName = "Ada" });
        Assert.Contains("\"Ada\"", yaml);
    }

    [Fact]
    public void ParseFile_Windows1252_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".yaml");
        try {
            var service = new YamlService(o => o.Encoding = Encoding.GetEncoding(1252));
            service.ExportToYaml(new Person { GivenName = "José" }, path);
            var back = service.ParseFile<Person>(path);
            Assert.Equal("José", back.GivenName);
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_Utf8Span_BindsPerson()
    {
        var bytes = YamlService.Default.ExportToYamlBytes(new Person { GivenName = "Ada", Age = 36 });
        var person = YamlService.Default.Parse<Person>(bytes.AsSpan());
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
        Assert.False(YamlService.Default.TryExport(new Person { GivenName = "Ada" }, new byte[1], out var written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void Default_ParallelRoundTrip_ReturnsTheDocument()
    {
        var errors = 0;
        Parallel.For(
            0, 32, _ => {
                try {
                    var yaml = YamlService.Default.ExportToYamlString(new Person { GivenName = "Ada", Age = 36 });
                    var person = YamlService.Default.ParseString<Person>(yaml);
                    if (person.GivenName != "Ada" || person.Age != 36)
                        Interlocked.Increment(ref errors);
                }
                catch {
                    Interlocked.Increment(ref errors);
                }
            });

        Assert.Equal(0, errors);
    }

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }
}