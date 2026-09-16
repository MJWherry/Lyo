using Lyo.Common.Core.Enums;

namespace Lyo.Toml.Tests;

public class FormattedFieldTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ExportFormattedToTomlString_AlphabeticalService_KeepsCallerOrderAndFormattedText()
    {
        var fields = new Dictionary<string, Func<Person, string>> {
            ["note"] = person => person.Note ?? "",
            ["fullName"] = person => person.GivenName + " Lovelace"
        };
        var toml = new TomlService(o => o.Order = MemberOrder.Alphabetical).ExportFormattedToTomlString(Sample(), fields);
        Assert.True(toml.IndexOf("note", StringComparison.Ordinal) < toml.IndexOf("fullName", StringComparison.Ordinal));
        Assert.Contains("Ada Lovelace", toml, StringComparison.Ordinal);
        Assert.DoesNotContain("givenName", toml, StringComparison.Ordinal);
    }

    private static Person[] Sample() => [new() { GivenName = "Ada", Note = "hi" }];
}
