using System.Text.Json;
using Lyo.Json;

namespace Lyo.Toml.Tests;

public class TomlServiceTests
{
    [Fact]
    public void ParseString_Person_MatchesJsonContract()
    {
        const string toml = """
                            givenName = "Ada"
                            age = 36
                            """;

        var person = new TomlService().ParseString<Person>(toml);
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
    }

    [Fact]
    public void ExportToTomlString_NullProperty_OmitsKey()
    {
        var toml = new TomlService().ExportToTomlString(new Person { GivenName = "Ada" });
        Assert.Contains("givenName", toml, StringComparison.Ordinal);
        Assert.DoesNotContain("note", toml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("age", toml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseString_ArrayOfTables_LoadsAsList()
    {
        const string toml = """
                            [[items]]
                            givenName = "Ada"

                            [[items]]
                            givenName = "Grace"
                            """;

        var people = new TomlService().ParseString<List<Person>>(toml);
        Assert.Equal(2, people.Count);
        Assert.Equal("Ada", people[0].GivenName);
        Assert.Equal("Grace", people[1].GivenName);
    }

    [Fact]
    public void ConvertToJson_RoundTrips()
    {
        var toml = new TomlService().ExportToTomlString(new Person { GivenName = "Ada", Age = 36 });
        var json = new TomlService().ConvertToJson(toml);
        var person = JsonSerializer.Deserialize<Person>(json, LyoJsonSerializerOptions.Create());
        Assert.Equal("Ada", person!.GivenName);
        Assert.Equal(36, person.Age);
    }

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }
}