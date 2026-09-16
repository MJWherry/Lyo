using System.Text;

namespace Lyo.Csv.Tests;

public class CsvServiceDefaultTests
{
    private sealed class Row
    {
        public string? Name { get; set; }

        public int Age { get; set; }
    }

    [Fact]
    public void Default_RoundTripsCommaSeparatedRow()
    {
        var csv = CsvService.Default.ExportToCsvString(new[] { new Row { Name = "Ada", Age = 36 } });
        var rows = CsvService.Default.ParseBytes<Row>(Encoding.UTF8.GetBytes(csv)).ToList();
        Assert.Equal("Ada", rows[0].Name);
        Assert.Equal(36, rows[0].Age);
    }

    [Fact]
    public void SetEncoding_RacingParse_UsesOneEncoding()
    {
        var csv = new CsvService();
        var text = "Name,Age\nAda,36\n";
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        try {
            File.WriteAllText(path, text, new UTF8Encoding(false));
            var errors = 0;
            Parallel.For(0, 32, _ => {
                try {
                    if (Random.Shared.Next(2) == 0)
                        csv.SetEncoding(Encoding.Latin1);
                    else
                        csv.SetEncoding(new UTF8Encoding(false));
                    var rows = csv.ParseFile<Row>(path).ToList();
                    if (rows.Count != 1 || rows[0].Name != "Ada" || rows[0].Age != 36)
                        Interlocked.Increment(ref errors);
                }
                catch {
                    Interlocked.Increment(ref errors);
                }
            });
            Assert.Equal(0, errors);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
