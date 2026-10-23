using System.Text;

namespace Lyo.Xlsx.Tests;

public class XlsxServiceDefaultTests
{
    [Fact]
    public void Default_RoundTripsSheet()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = XlsxService.Default.ExportToXlsxBytes(new[] { new Person { Name = "Ada" } });
        var table = XlsxService.Default.ParseXlsxBytesAsDataTable(bytes).ValueOrThrow();
        Assert.Equal("Ada", table.Rows[0][0].DisplayValue);
    }

    [Fact]
    public void Default_ParallelRoundTrip_ReturnsTheSheet()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var errors = 0;
        Parallel.For(
            0, 4, _ => {
                try {
                    var bytes = XlsxService.Default.ExportToXlsxBytes(new[] { new Person { Name = "Ada" } });
                    var table = XlsxService.Default.ParseXlsxBytesAsDataTable(bytes).ValueOrThrow();
                    if (table.Rows[0][0].DisplayValue != "Ada")
                        Interlocked.Increment(ref errors);
                }
                catch {
                    Interlocked.Increment(ref errors);
                }
            });

        Assert.Equal(0, errors);
    }

    private sealed class Person
    {
        public string? Name { get; set; }
    }
}