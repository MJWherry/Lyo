using Lyo.Common.Core.Enums;
using Lyo.Xlsx.Models;

namespace Lyo.Xlsx.Tests;

public class MemberOrderTests
{
    private sealed class Person
    {
        public string? GivenName { get; set; }

        public int Age { get; set; }

        public string? Note { get; set; }
    }

    [Fact]
    public void ExportToXlsxBytes_OrderAlphabetical_WritesAgeBeforeGivenName()
    {
        var options = new XlsxOptions { Order = MemberOrder.Alphabetical };
        var service = new XlsxService(options: options);
        var bytes = service.ExportToXlsxBytes(new[] { new Person { GivenName = "Ada", Age = 36, Note = "hi" } });
        var table = service.ParseXlsxBytesAsDataTable(bytes).ValueOrThrow();
        Assert.Equal("Age", table.Headers[0].DisplayValue);
        Assert.Equal("GivenName", table.Headers[1].DisplayValue);
        Assert.Equal("Note", table.Headers[2].DisplayValue);
        Assert.Equal("Ada", table.Rows[0][1].DisplayValue);
    }

    [Fact]
    public void ExportToXlsxBytes_OrderNames_WritesNoteBeforeGivenName()
    {
        var options = new XlsxOptions { Order = MemberOrder.Specified };
        options.OrderNames.Add("Note");
        options.OrderNames.Add("GivenName");
        var service = new XlsxService(options: options);
        var bytes = service.ExportToXlsxBytes(new[] { new Person { GivenName = "Ada", Age = 36, Note = "hi" } });
        var table = service.ParseXlsxBytesAsDataTable(bytes).ValueOrThrow();
        Assert.Equal("Note", table.Headers[0].DisplayValue);
        Assert.Equal("GivenName", table.Headers[1].DisplayValue);
        Assert.Equal("Age", table.Headers[2].DisplayValue);
        Assert.Equal("Ada", table.Rows[0][1].DisplayValue);
    }
}
