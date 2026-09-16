using System.Text;
using Lyo.Json;

namespace Lyo.Toml.Tests;

public class TomlOptionsTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public Address? Address { get; set; }

        public DateTimeOffset When { get; set; }
    }

    private sealed class Address
    {
        public string? City { get; set; }
    }

    [Fact]
    public void ParseString_ItemsKeyPeople_LoadsList()
    {
        const string toml = """
            [[people]]
            givenName = "Ada"
            """;
        var people = new TomlService(o => o.ItemsKey = "people").ParseString<List<Person>>(toml);
        Assert.Single(people);
        Assert.Equal("Ada", people[0].GivenName);
    }

    [Fact]
    public void ExportToTomlString_SnakeCase_WritesGivenNameKey()
    {
        var toml = new TomlService(o => o.Naming = DocumentNaming.SnakeCase).ExportToTomlString(new Person { GivenName = "Ada" });
        Assert.Contains("given_name", toml);
    }

    [Fact]
    public void ExportToTomlString_WriteNumbersAsStrings_QuotesInteger()
    {
        var toml = new TomlService(o => o.WriteNumbersAsStrings = true).ExportToTomlString(new Person { Age = 36 });
        Assert.Contains("\"36\"", toml);
    }

    [Fact]
    public void ExportToTomlString_InlineTables_RoundTripsAddressAndKeepsArrayOfTables()
    {
        var service = new TomlService(o => o.NestedTables = TomlNestedTables.Inline);
        var toml = service.ExportToTomlString(new Person { Address = new Address { City = "London" } });
        Assert.Contains("address = {", toml);
        Assert.Contains("London", toml);
        var back = service.ParseString<Person>(toml);
        Assert.Equal("London", back.Address!.City);
        var people = service.ExportToTomlString(new List<Person> { new() { GivenName = "Ada" } });
        Assert.Contains("[[items]]", people);
    }

    [Fact]
    public void ExportToTomlString_LocalDate_OmitsOffset()
    {
        var when = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var toml = new TomlService(o => o.DateStyle = TomlDateStyle.Local).ExportToTomlString(new Person { When = when });
        Assert.Contains("2020-01-02T03:04:05", toml);
        Assert.DoesNotContain("+00:00", toml);
    }

    [Fact]
    public void Constructor_IgnoreNullsFalse_Throws()
        => Assert.ThrowsAny<Exception>(() => new TomlService(o => o.IgnoreNulls = false));

    [Fact]
    public void ParseFile_Windows1252_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".toml");
        try {
            var service = new TomlService(o => o.Encoding = Encoding.GetEncoding(1252));
            service.ExportToToml(new Person { GivenName = "José" }, path);
            var back = service.ParseFile<Person>(path);
            Assert.Equal("José", back.GivenName);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_Utf8Span_BindsPerson()
    {
        var bytes = TomlService.Default.ExportToTomlBytes(new Person { GivenName = "Ada", Age = 36 });
        var person = TomlService.Default.Parse<Person>(bytes.AsSpan());
        Assert.Equal("Ada", person.GivenName);
        Assert.Equal(36, person.Age);
        Assert.False(TomlService.Default.TryExport(new Person { GivenName = "Ada" }, new byte[1], out var written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void Default_ParallelRoundTrip_ReturnsTheDocument()
    {
        var errors = 0;
        Parallel.For(0, 32, _ => {
            try {
                var toml = TomlService.Default.ExportToTomlString(new Person { GivenName = "Ada", Age = 36 });
                var person = TomlService.Default.ParseString<Person>(toml);
                if (person.GivenName != "Ada" || person.Age != 36)
                    Interlocked.Increment(ref errors);
            }
            catch { Interlocked.Increment(ref errors); }
        });
        Assert.Equal(0, errors);
    }
}
