using Lyo.Common.Core.Enums;

namespace Lyo.Yaml.Tests;

public class FormattedFieldTests
{
    [Fact]
    public void ExportFormattedToYamlString_AlphabeticalService_KeepsCallerOrderAndFormattedText()
    {
        var fields = new Dictionary<string, Func<Person, string>> { ["note"] = person => person.Note ?? "", ["fullName"] = person => person.GivenName + " Lovelace" };
        var yaml = new YamlService(o => o.Order = MemberOrder.Alphabetical).ExportFormattedToYamlString(Sample(), fields);
        Assert.True(yaml.IndexOf("note:", StringComparison.Ordinal) < yaml.IndexOf("fullName:", StringComparison.Ordinal));
        Assert.Contains("Ada Lovelace", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("givenName", yaml, StringComparison.Ordinal);
    }

    private static Person[] Sample() => [new() { GivenName = "Ada", Note = "hi" }];

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public string? Note { get; set; }
    }
}