using System.Text;
using Lyo.Json;

namespace Lyo.Xml.Tests;

public class XmlOptionsTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ConvertToJson_ReadNumbersFromStringsFalse_EmitsQuotedAge()
    {
        const string xml = "<person><age>36</age></person>";
        var json = new XmlService(o => o.ReadNumbersFromStrings = false).ConvertToJson(xml);
        Assert.Contains("\"36\"", json);
    }

    [Fact]
    public void ExportToXmlString_RootNameAndNamespace_ReplacePerson()
    {
        var xml = new XmlService(o => {
            o.RootElementName = "customer";
            o.Namespace = "urn:lyo:person";
        }).ExportToXmlString(new Person { GivenName = "Ada" });
        Assert.Contains("<customer", xml);
        Assert.Contains("xmlns=\"urn:lyo:person\"", xml);
        Assert.DoesNotContain("<person", xml);
    }

    [Fact]
    public void ExportToXmlString_Declaration_IncludesUtf8()
    {
        var xml = new XmlService(o => o.OmitXmlDeclaration = false).ExportToXmlString(new Person { GivenName = "Ada" });
        Assert.Contains("encoding=\"utf-8\"", xml);
    }

    [Fact]
    public void ParseFile_Utf16_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
        try {
            var service = new XmlService(o => o.Encoding = Encoding.Unicode);
            service.ExportToXml(new Person { GivenName = "Ada", Age = 36 }, path);
            var back = service.ParseFile<Person>(path);
            Assert.Equal("Ada", back.GivenName);
            Assert.Equal(36, back.Age);
        }
        finally { File.Delete(path); }
    }

    private sealed class AttributedPerson
    {
        public string? GivenName { get; set; }

        [XmlMember(AsAttribute = true)]
        public int? Age { get; set; }
    }

    private sealed class Address
    {
        [XmlMember(AsAttribute = true)]
        public string? Id { get; set; }

        public string? City { get; set; }
    }

    private sealed class NestedPerson
    {
        public string? GivenName { get; set; }

        public Address? Address { get; set; }
    }

    private sealed class ForcedElement
    {
        public string? GivenName { get; set; }

        [XmlMember(AsAttribute = false)]
        public int? Age { get; set; }
    }

    [Fact]
    public void ExportToXmlString_XmlMemberAttribute_WritesAgeAsAttribute()
    {
        var service = new XmlService();
        var xml = service.ExportToXmlString(new AttributedPerson { GivenName = "Ada", Age = 36 });
        Assert.Contains("age=\"36\"", xml);
        Assert.Contains("<givenName>Ada</givenName>", xml);
        var back = service.ParseString<AttributedPerson>(xml);
        Assert.Equal("Ada", back.GivenName);
        Assert.Equal(36, back.Age);
    }

    [Fact]
    public void ExportToXmlString_TypedMemberConfig_WritesAgeAsAttribute()
    {
        var service = new XmlService(o => o.Member<Person>(nameof(Person.Age), asAttribute: true));
        var xml = service.ExportToXmlString(new Person { GivenName = "Ada", Age = 36 });
        Assert.Contains("age=\"36\"", xml);
        Assert.Contains("<givenName>Ada</givenName>", xml);
        var back = service.ParseString<Person>(xml);
        Assert.Equal("Ada", back.GivenName);
        Assert.Equal(36, back.Age);
    }

    [Fact]
    public void ExportToXmlString_NameMemberConfig_WritesAgeAttributeFromJson()
    {
        var xml = new XmlService(o => o.Member("age", asAttribute: true)).ConvertFromJson("{\"age\":36,\"givenName\":\"Ada\"}");
        Assert.Contains("age=\"36\"", xml);
        Assert.Contains("<givenName>Ada</givenName>", xml);
    }

    [Fact]
    public void ExportToXmlString_NestedXmlMember_WritesIdOnAddress()
    {
        var xml = new XmlService().ExportToXmlString(new NestedPerson { GivenName = "Ada", Address = new Address { Id = "a1", City = "London" } });
        Assert.Contains("<address id=\"a1\">", xml);
        Assert.Contains("<city>London</city>", xml);
        Assert.Contains("<givenName>Ada</givenName>", xml);
    }

    [Fact]
    public void ExportToXmlString_ExplicitElement_WinsOverScalarsAsAttributes()
    {
        var xml = new XmlService(o => o.ScalarsAsAttributes = true).ExportToXmlString(new ForcedElement { GivenName = "Ada", Age = 36 });
        Assert.Contains("givenName=\"Ada\"", xml);
        Assert.Contains("<age>36</age>", xml);
        Assert.DoesNotContain("age=\"36\"", xml);
    }

    [Fact]
    public void ExportToXmlString_ScalarsAsAttributes_RoundTrips()
    {
        var service = new XmlService(o => o.ScalarsAsAttributes = true);
        var xml = service.ExportToXmlString(new Person { GivenName = "Ada", Age = 36 });
        Assert.Contains("givenName=\"Ada\"", xml);
        var back = service.ParseString<Person>(xml);
        Assert.Equal("Ada", back.GivenName);
        Assert.Equal(36, back.Age);
    }

    [Fact]
    public void ExportToXmlString_SnakeCase_WritesGivenNameElement()
    {
        var xml = new XmlService(o => o.Naming = DocumentNaming.SnakeCase).ExportToXmlString(new Person { GivenName = "Ada" });
        Assert.Contains("<given_name>Ada</given_name>", xml);
    }

    [Fact]
    public void Parse_Utf8Span_BindsPerson()
    {
        var bytes = XmlService.Default.ExportToXmlBytes(new Person { GivenName = "Ada", Age = 36 });
        var person = XmlService.Default.Parse<Person>(bytes.AsSpan());
        Assert.Equal("Ada", person.GivenName);
        Assert.False(XmlService.Default.TryExport(new Person { GivenName = "Ada" }, new byte[1], out var written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void Default_ParallelRoundTrip_ReturnsTheDocument()
    {
        var errors = 0;
        Parallel.For(0, 32, _ => {
            try {
                var xml = XmlService.Default.ExportToXmlString(new Person { GivenName = "Ada", Age = 36 });
                var person = XmlService.Default.ParseString<Person>(xml);
                if (person.GivenName != "Ada" || person.Age != 36)
                    Interlocked.Increment(ref errors);
            }
            catch { Interlocked.Increment(ref errors); }
        });
        Assert.Equal(0, errors);
    }
}
