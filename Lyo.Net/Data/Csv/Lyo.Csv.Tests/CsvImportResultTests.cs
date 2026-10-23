using Lyo.Csv.Tests.TestModels;

namespace Lyo.Csv.Tests;

public class CsvImportResultTests
{
    [Fact]
    public void Parse_Csv_TwoBadRows_ReturnsThreeGoodRows()
    {
        const string csv = """
                           Id,Name,Age
                           1,Alice,30
                           2,Bob,nope
                           3,Charlie,40
                           4,Dana,nope
                           5,Eve,22
                           """;

        var result = new CsvService().ParseStringAsResult<Person>(csv);
        Assert.True(result.IsSuccess);
        Assert.Equal(["Alice", "Charlie", "Eve"], result.Data!.Select(row => row.Name!).ToArray());
        Assert.Equal([3, 5], result.ParseErrors!.Select(error => error.RowNumber).ToArray());
    }

    [Fact]
    public void Parse_Csv_HeaderOnly_ReturnsZeroRows()
    {
        var result = new CsvService().ParseStringAsResult<Person>("Id,Name,Age");
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
        Assert.True(result.ParseErrors is null || result.ParseErrors.Count == 0);
    }
}