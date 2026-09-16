using Lyo.Common.Core.Attributes;
using Lyo.Common.Core.Enums;

namespace Lyo.Json.Tests;

public class MemberOrderTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        public string? Note { get; set; }
    }

    private sealed class OrderedPerson
    {
        [MemberOrder(1)]
        public string? GivenName { get; set; }

        public int? Age { get; set; }

        [MemberOrder(0)]
        public string? Note { get; set; }
    }

    [Fact]
    public void ExportToJsonString_OrderDefined_WritesGivenNameBeforeAge()
    {
        var json = new JsonService().ExportToJsonString(Sample());
        Assert.True(Index(json, "givenName") < Index(json, "age"));
    }

    [Fact]
    public void ExportToJsonString_OrderAlphabetical_WritesAgeBeforeGivenName()
    {
        var json = new JsonService(o => o.Order = MemberOrder.Alphabetical).ExportToJsonString(Sample());
        Assert.True(Index(json, "age") < Index(json, "givenName"));
        Assert.True(Index(json, "givenName") < Index(json, "note"));
    }

    [Fact]
    public void ExportToJsonString_MemberOrderAttribute_WritesNoteBeforeGivenName()
    {
        var json = new JsonService(o => o.Order = MemberOrder.Specified).ExportToJsonString(new OrderedPerson { GivenName = "Ada", Age = 36, Note = "hi" });
        Assert.True(Index(json, "note") < Index(json, "givenName"));
        Assert.True(Index(json, "givenName") < Index(json, "age"));
    }

    [Fact]
    public void ExportToJsonString_OrderNames_WritesNoteBeforeGivenName()
    {
        var json = new JsonService(o => {
            o.Order = MemberOrder.Specified;
            o.OrderNames.Add("note");
            o.OrderNames.Add("givenName");
        }).ExportToJsonString(Sample());
        Assert.True(Index(json, "note") < Index(json, "givenName"));
        Assert.True(Index(json, "givenName") < Index(json, "age"));
    }

    private static Person Sample() => new() { GivenName = "Ada", Age = 36, Note = "hi" };

    private static int Index(string json, string name) => json.IndexOf("\"" + name + "\"", StringComparison.Ordinal);
}
