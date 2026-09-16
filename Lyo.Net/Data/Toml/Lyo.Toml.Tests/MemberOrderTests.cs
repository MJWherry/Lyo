using Lyo.Common.Core.Enums;

namespace Lyo.Toml.Tests;

public class MemberOrderTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ExportToTomlString_OrderAlphabetical_WritesAgeBeforeGivenName()
    {
        var toml = new TomlService(o => o.Order = MemberOrder.Alphabetical).ExportToTomlString(new Person { GivenName = "Ada", Age = 36, Note = "hi" });
        Assert.True(toml.IndexOf("age", StringComparison.Ordinal) < toml.IndexOf("givenName", StringComparison.Ordinal));
    }
}
