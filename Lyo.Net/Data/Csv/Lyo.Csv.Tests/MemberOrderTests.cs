using System.Text;
using Lyo.Common.Core.Enums;
using Lyo.Csv.Models;

namespace Lyo.Csv.Tests;

public class MemberOrderTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int Age { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ExportToCsvString_OrderAlphabetical_WritesAgeBeforeGivenName()
    {
        var options = new CsvOptions { Order = MemberOrder.Alphabetical };
        var csv = new CsvService(options: options).ExportToCsvString(new[] { new Person { GivenName = "Ada", Age = 36, Note = "hi" } });
        Assert.StartsWith("Age,GivenName,Note", Header(csv));
        var back = new CsvService(options: options).ParseBytes<Person>(Encoding.UTF8.GetBytes(csv)).Single();
        Assert.Equal("Ada", back.GivenName);
    }

    [Fact]
    public void ExportToCsvString_OrderNames_WritesNoteBeforeGivenName()
    {
        var options = new CsvOptions { Order = MemberOrder.Specified };
        options.OrderNames.Add("Note");
        options.OrderNames.Add("GivenName");
        var csv = new CsvService(options: options).ExportToCsvString(new[] { new Person { GivenName = "Ada", Age = 36, Note = "hi" } });
        Assert.StartsWith("Note,GivenName,Age", Header(csv));
        var back = new CsvService(options: options).ParseBytes<Person>(Encoding.UTF8.GetBytes(csv)).Single();
        Assert.Equal("Ada", back.GivenName);
    }

    private static string Header(string csv) => csv.Replace("\r", "").Split('\n')[0];
}
