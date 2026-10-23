using ClosedXML.Excel;
using Lyo.Xlsx.Tests.TestModels;

namespace Lyo.Xlsx.Tests;

public class XlsxImportResultTests
{
    [Fact]
    public void Parse_Xlsx_UnknownSheet_IsDocumentError()
    {
        var bytes = Workbook(sheet => {
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Name";
            sheet.Cell(2, 1).Value = 1;
            sheet.Cell(2, 2).Value = "Alice";
        });

        var result = new XlsxService().ParseBytesAsResult<TestModel>(bytes, "Missing");
        Assert.False(result.IsSuccess);
        Assert.True(result.Data is null || result.Data.Count == 0);
    }

    [Fact]
    public void Parse_Xlsx_BadCell_SkipsThatRowOnly()
    {
        var bytes = Workbook(sheet => {
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Name";
            sheet.Cell(1, 3).Value = "Age";
            Write(sheet, 2, 1, "Alice", 30);
            Write(sheet, 3, 2, "Bob", "nope");
            Write(sheet, 4, 3, "Charlie", 40);
            Write(sheet, 5, 4, "Dana", "nope");
            Write(sheet, 6, 5, "Eve", 22);
        });

        var result = new XlsxService().ParseBytesAsResult<TestModel>(bytes);
        Assert.True(result.IsSuccess);
        Assert.Equal(["Alice", "Charlie", "Eve"], result.Data!.Select(row => row.Name!).ToArray());
        Assert.Equal(2, result.ParseErrors!.Count);
    }

    [Fact]
    public void Parse_Xlsx_MissingRequiredHeader_IsDocumentError()
    {
        var bytes = Workbook(sheet => sheet.Cell(1, 1).Value = "Id");
        var result = new XlsxService().ParseBytesAsResult<TestModel>(bytes, requiredHeaders: ["Name"]);
        Assert.False(result.IsSuccess);
        Assert.True(result.Data is null || result.Data.Count == 0);
    }

    private static void Write(IXLWorksheet sheet, int row, int id, string name, object age)
    {
        sheet.Cell(row, 1).Value = id;
        sheet.Cell(row, 2).Value = name;
        sheet.Cell(row, 3).Value = age.ToString()!;
    }

    private static byte[] Workbook(Action<IXLWorksheet> fill, string sheetName = "Sheet1")
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);
        fill(sheet);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}