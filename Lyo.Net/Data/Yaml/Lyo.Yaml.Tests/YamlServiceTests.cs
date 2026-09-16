using System.Text;
using System.Text.Json;
using Lyo.Json;

namespace Lyo.Yaml.Tests;

public class YamlServiceTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ParseString_Person_MatchesJsonContract()
    {
        const string yaml = """
            givenName: Ada
            age: 36
            """;
        var person = new YamlService().ParseString<Person>(yaml);
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
    }

    [Fact]
    public void ConvertToJson_Object_MatchesJsonSerializer()
    {
        var person = new Person { GivenName = "Ada", Age = 36 };
        var yaml = new YamlService().ExportToYamlString(person);
        var json = new YamlService().ConvertToJson(yaml);
        var expected = JsonSerializer.Serialize(person, LyoJsonSerializerOptions.Create());
        Assert.Equal(expected, json);
    }

    [Fact]
    public void ConvertFromJson_RoundTripsThroughYaml()
    {
        var person = new Person { GivenName = "Ada", Age = 36 };
        var json = JsonSerializer.Serialize(person, LyoJsonSerializerOptions.Create());
        var yaml = new YamlService().ConvertFromJson(json);
        var back = new YamlService().ParseString<Person>(yaml);
        Assert.Equal("Ada", back.GivenName);
        Assert.Equal(36, back.Age);
        Assert.Null(back.Note);
    }

    [Fact]
    public void ConvertToJson_Stream_WritesJson()
    {
        var yaml = new YamlService().ExportToYamlString(new Person { GivenName = "Ada", Age = 36 });
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(yaml));
        using var output = new MemoryStream();
        new YamlService().ConvertToJson(input, output);
        var json = Encoding.UTF8.GetString(output.ToArray());
        var person = JsonSerializer.Deserialize<Person>(json, LyoJsonSerializerOptions.Create());
        Assert.Equal("Ada", person!.GivenName);
        Assert.Equal(36, person.Age);
    }
}
