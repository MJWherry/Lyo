using Lyo.Common.Core.Identifiers;

namespace Lyo.Json.Tests;

public class JsonServiceTests
{
    [Fact]
    public void ParseString_Person_BindsCamelCase()
    {
        var person = new JsonService().ParseString<Person>("""{"givenName":"Ada"}""");
        Assert.Equal("Ada", person.GivenName);
    }

    [Fact]
    public void ExportToJsonString_Person_OmitsNull()
    {
        var json = new JsonService().ExportToJsonString(new Person { GivenName = "Ada" });
        Assert.Contains("\"givenName\":\"Ada\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("age", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("note", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportToJsonStream_RoundTrips()
    {
        var svc = new JsonService();
        using var stream = new MemoryStream();
        svc.ExportToJson(new Person { GivenName = "Ada", Age = 36 }, stream);
        stream.Position = 0;
        var person = svc.ParseStream<Person>(stream);
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
    }

    [Fact]
    public void ParseString_List_BindsTwoPeople()
    {
        var people = new JsonService().ParseString<List<Person>>("""[{"givenName":"Ada"},{"givenName":"Grace"}]""");
        Assert.Equal(2, people.Count);
        Assert.Equal("Ada", people[0].GivenName);
        Assert.Equal("Grace", people[1].GivenName);
    }

    [Fact]
    public void ParseBytes_Person_RoundTrips()
    {
        var svc = new JsonService();
        var bytes = svc.ExportToJsonBytes(new Person { GivenName = "Ada", Age = 36 });
        var person = svc.ParseBytes<Person>(bytes);
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
    }

    [Fact]
    public void ParseFile_Person_RoundTrips()
    {
        var svc = new JsonService();
        var path = Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7() + ".json");
        try {
            svc.ExportToJson(new Person { GivenName = "Ada", Age = 36 }, path);
            var person = svc.ParseFile<Person>(path);
            Assert.Equal("Ada", person.GivenName);
            Assert.Equal(36, person.Age);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseStreamAsync_Person_RoundTrips()
    {
        var svc = new JsonService();
        await using var stream = new MemoryStream();
        await svc.ExportToJsonAsync(new Person { GivenName = "Ada", Age = 36 }, stream, TestCt);
        stream.Position = 0;
        var person = await svc.ParseStreamAsync<Person>(stream, TestCt);
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
    }

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }
}