using System.Text.Json;
using Lyo.Json;

namespace Lyo.Xml.Tests;

public class XmlServiceTests
{
    [Fact]
    public void ParseString_Person_MatchesJsonContract()
    {
        const string xml = """
                           <person>
                             <givenName>Ada</givenName>
                             <age>36</age>
                           </person>
                           """;

        var person = new XmlService().ParseString<Person>(xml);
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
    }

    [Fact]
    public void ExportToXmlString_UsesChildElements()
    {
        var xml = new XmlService().ExportToXmlString(new Person { GivenName = "Ada", Age = 36 });
        Assert.Contains("<person>", xml, StringComparison.Ordinal);
        Assert.Contains("<givenName>Ada</givenName>", xml, StringComparison.Ordinal);
        Assert.Contains("<age>36</age>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseString_RepeatedElement_LoadsAsList()
    {
        const string xml = """
                           <person>
                             <givenName>Ada</givenName>
                             <tags>a</tags>
                             <tags>b</tags>
                           </person>
                           """;

        var person = new XmlService().ParseString<Person>(xml);
        Assert.Equal(["a", "b"], person.Tags);
    }

    [Fact]
    public void ConvertToJson_RoundTrips()
    {
        var xml = new XmlService().ExportToXmlString(new Person { GivenName = "Ada", Age = 36 });
        var json = new XmlService().ConvertToJson(xml);
        var person = JsonSerializer.Deserialize<Person>(json, LyoJsonSerializerOptions.Create());
        Assert.Equal("Ada", person!.GivenName);
        Assert.Equal(36, person.Age);
    }

    [Fact]
    public void ExportToXmlString_NullProperty_OmitsElement()
    {
        var xml = new XmlService().ExportToXmlString(new Person { GivenName = "Ada" });
        Assert.DoesNotContain("note", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("age", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tags", xml, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }

        public List<string>? Tags { get; set; }
    }
}