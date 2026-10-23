using Lyo.Common.Core.Enums;

namespace Lyo.Yaml.Tests;

public class MemberOrderTests
{
    [Fact]
    public void ExportToYamlString_OrderAlphabetical_WritesAgeBeforeGivenName()
    {
        var yaml = new YamlService(o => o.Order = MemberOrder.Alphabetical).ExportToYamlString(new Person { GivenName = "Ada", Age = 36, Note = "hi" });
        Assert.True(yaml.IndexOf("age:", StringComparison.Ordinal) < yaml.IndexOf("givenName:", StringComparison.Ordinal));
    }

    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }
}