using Lyo.Common.Core.Enums;

namespace Lyo.Xml.Tests;

public class MemberOrderTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ExportToXmlString_OrderAlphabetical_WritesAgeBeforeGivenName()
    {
        var xml = new XmlService(o => o.Order = MemberOrder.Alphabetical).ExportToXmlString(new Person { GivenName = "Ada", Age = 36, Note = "hi" });
        Assert.True(xml.IndexOf("<age>", StringComparison.Ordinal) < xml.IndexOf("<givenName>", StringComparison.Ordinal));
    }
}
