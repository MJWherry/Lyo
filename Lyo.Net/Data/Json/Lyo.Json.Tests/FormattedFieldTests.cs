using System.Reflection;
using Lyo.Common.Core.Enums;

namespace Lyo.Json.Tests;

public class FormattedFieldTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int Age { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ExportFormattedToJsonString_AlphabeticalService_KeepsCallerOrderAndFormattedText()
    {
        var json = new JsonService(o => o.Order = MemberOrder.Alphabetical).ExportFormattedToJsonString(Sample(), Fields());
        Assert.True(json.IndexOf("\"note\"", StringComparison.Ordinal) < json.IndexOf("\"fullName\"", StringComparison.Ordinal));
        Assert.Contains("Ada Lovelace", json, StringComparison.Ordinal);
        Assert.DoesNotContain("givenName", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportFormattedToJsonString_EmptyFormatter_KeepsTheField()
    {
        var fields = new Dictionary<string, Func<Person, string>> { ["note"] = _ => "" };
        var json = new JsonService(o => o.IgnoreEmptyStrings = true).ExportFormattedToJsonString(Sample(), fields);
        Assert.Contains("\"note\":\"\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportColumnsToJsonString_AlphabeticalService_KeepsHeaderOrderAndNumbers()
    {
        var columns = new Dictionary<string, PropertyInfo> {
            ["note"] = typeof(Person).GetProperty(nameof(Person.Note))!,
            ["age"] = typeof(Person).GetProperty(nameof(Person.Age))!
        };
        var json = new JsonService(o => o.Order = MemberOrder.Alphabetical).ExportColumnsToJsonString(Sample(), columns);
        Assert.True(json.IndexOf("\"note\"", StringComparison.Ordinal) < json.IndexOf("\"age\"", StringComparison.Ordinal));
        Assert.Contains("\"age\":36", json, StringComparison.Ordinal);
        Assert.DoesNotContain("givenName", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportFormattedToJsonString_EmptyFields_Throws()
    {
        var fields = new Dictionary<string, Func<Person, string>>();
        Assert.Throws<ArgumentException>(() => new JsonService().ExportFormattedToJsonString(Sample(), fields));
    }

    private static Person[] Sample() => [new() { GivenName = "Ada", Age = 36, Note = "hi" }];

    private static Dictionary<string, Func<Person, string>> Fields()
        => new() {
            ["note"] = person => person.Note ?? "",
            ["fullName"] = person => person.GivenName + " Lovelace"
        };
}
