using Lyo.Common.Core.Enums;

namespace Lyo.Xml.Tests;

public class FormattedFieldTests
{
    [Fact]
    public void ExportFormattedToXmlString_AlphabeticalService_KeepsCallerOrderAndFormattedText()
    {
        var fields = new Dictionary<string, Func<Person, string>> { ["note"] = person => person.Note ?? "", ["fullName"] = person => person.GivenName + " Lovelace" };
        var xml = new XmlService(o => o.Order = MemberOrder.Alphabetical).ExportFormattedToXmlString(Sample(), fields);
        Assert.True(xml.IndexOf("<note>", StringComparison.Ordinal) < xml.IndexOf("<fullName>", StringComparison.Ordinal));
        Assert.Contains("Ada Lovelace", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("givenName", xml, StringComparison.Ordinal);
    }

    private static Person[] Sample() => [new() { GivenName = "Ada", Note = "hi" }];

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public string? Note { get; set; }
    }
}