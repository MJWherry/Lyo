using System.Reflection;
using System.Text;
using ExcelDataReader;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Lyo.Xlsx.Tests.TestModels;
using Microsoft.Extensions.Logging;

namespace Lyo.Xlsx.Tests;

public class XlsxServiceTests : IDisposable, IAsyncDisposable
{
    private readonly ILogger<XlsxService> _logger;

    private readonly IOTempSession _tempSession;

    public XlsxServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<XlsxService>();
        _tempSession = new(new() { FileExtension = ".xlsx" }, loggerFactory.CreateLogger<IOTempSession>());
    }

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

    private static MemoryStream BytesToStream(byte[]? b) => new(b ?? []);

    private static void EnsureCodePages() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [Fact]
    public void ExportSelectedProperties_ToXlsxBytes_And_ParseXlsxStreamAsDictionary_RoundTrip()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "Alice", Age = 30 }, new() { Id = 2, Name = "Bob", Age = 25 }];
        List<PropertyInfo> props = [typeof(TestModel).GetProperty(nameof(TestModel.Name))!, typeof(TestModel).GetProperty(nameof(TestModel.Age))!];
        var bytes = svc.ExportToXlsxBytes(data, props);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        // Parse produced xlsx back into dictionary of rows
        using var ms = BytesToStream(bytes);
        var dict = svc.ParseXlsxStreamAsDictionary(ms);
        Assert.Equal(2, dict.Count);
        Assert.Equal("Alice", dict[0][0]);
        Assert.Equal("30", dict[0][1]);
        Assert.Equal("Bob", dict[1][0]);
        Assert.Equal("25", dict[1][1]);
    }

    [Fact]
    public void ParseXlsxStreamAsDataTable_IncludesHeadersFromFirstRow()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "Alice", Age = 30 }, new() { Id = 2, Name = "Bob", Age = 25 }];
        var bytes = svc.ExportToXlsxBytes(data);
        using var ms = BytesToStream(bytes);
        var result = svc.ParseXlsxStreamAsDataTable(ms);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal(3, dt.Headers.Count);
        Assert.Equal("Id", dt.Headers[0].DisplayValue);
        Assert.Equal("Name", dt.Headers[1].DisplayValue);
        Assert.Equal("Age", dt.Headers[2].DisplayValue);
        Assert.True(dt.Headers[0].FontBold == true);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("1", dt.Rows[0][0].DisplayValue);
        Assert.Equal("Alice", dt.Rows[0][1].DisplayValue);
        Assert.Equal("30", dt.Rows[0][2].DisplayValue);
        Assert.Equal("2", dt.Rows[1][0].DisplayValue);
        Assert.Equal("Bob", dt.Rows[1][1].DisplayValue);
        Assert.Equal("25", dt.Rows[1][2].DisplayValue);
    }

    [Fact]
    public void ParseXlsxBytesAsDataTable_ReturnsDataTable()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 7, Name = "Test", Age = 42 }];
        var bytes = svc.ExportToXlsxBytes(data);
        var result = svc.ParseXlsxBytesAsDataTable(bytes);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal(3, dt.Headers.Count);
        Assert.Equal("7", dt.Rows[0][0].DisplayValue);
        Assert.Equal("Test", dt.Rows[0][1].DisplayValue);
        Assert.Equal("42", dt.Rows[0][2].DisplayValue);
    }

    [Fact]
    public void ParseXlsxFileAsDataTable_ReturnsDataTable()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 9, Name = "FileTest", Age = 99 }];
        var bytes = svc.ExportToXlsxBytes(data);
        var path = _tempSession.GetFilePath("parse-datatable.xlsx");
        File.WriteAllBytes(path, bytes);
        var result = svc.ParseXlsxFileAsDataTable(path);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal(3, dt.Headers.Count);
        Assert.Equal("9", dt.Rows[0][0].DisplayValue);
        Assert.Equal("FileTest", dt.Rows[0][1].DisplayValue);
    }

    [Fact]
    public void ExportToHtmlTable_ProducesValidHtmlWithTable()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "HtmlTest", Age = 50 }];
        var bytes = svc.ExportToXlsxBytes(data);
        var html = svc.ExportToHtmlTable(bytes);
        Assert.NotNull(html);
        Assert.Contains("<!DOCTYPE", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<table", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Id", html);
        Assert.Contains("Name", html);
        Assert.Contains("HtmlTest", html);
    }

    [Fact]
    public void BatchParseFilesAsDataTable_ReturnsResultPerFile()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data1 = [new() { Id = 1, Name = "A", Age = 10 }];
        TestModel[] data2 = [new() { Id = 2, Name = "B", Age = 20 }];
        var path1 = _tempSession.GetFilePath("batch1.xlsx");
        var path2 = _tempSession.GetFilePath("batch2.xlsx");
        File.WriteAllBytes(path1, svc.ExportToXlsxBytes(data1));
        File.WriteAllBytes(path2, svc.ExportToXlsxBytes(data2));
        var results = svc.BatchParseFilesAsDataTable([path1, path2]);
        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        var dt1 = results[0].ValueOrThrow();
        Assert.Equal("A", dt1.Rows[0][1].DisplayValue);
        Assert.True(results[1].IsSuccess);
        var dt2 = results[1].ValueOrThrow();
        Assert.Equal("B", dt2.Rows[0][1].DisplayValue);
    }

    [Fact]
    public void ExportMultiSheet_ProducesMultipleTables()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] a = [new() { Id = 1, Name = "A", Age = 10 }];
        TestModel[] b = [new() { Id = 2, Name = "B", Age = 20 }];
        var dataSets = new Dictionary<string, IEnumerable<TestModel>> { { "SheetA", a }, { "SheetB", b } };
        var bytes = svc.ExportToXlsxBytes(dataSets);
        using var ms = BytesToStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(ms);
        var config = new ExcelDataSetConfiguration();
        config.ConfigureDataTable = _ => new() { UseHeaderRow = true };
        var ds = reader.AsDataSet(config);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal("SheetA", ds.Tables[0].TableName);
        Assert.Equal("SheetB", ds.Tables[1].TableName);
        // Validate first table has one data row with Name=A
        Assert.Equal(1, ds.Tables[0].Rows.Count);
        Assert.Equal("A", ds.Tables[0].Rows[0]["Name"].ToString());
    }

    [Fact]
    public void ConvertXlsxToCsv_ProducesQuotedFieldsForCommasAndQuotes()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "Smith, \"John\"", Age = 40 }];
        // Export to xlsx bytes
        var xlsx = svc.ExportToXlsxBytes(data);
        // Convert to CSV using the service
        using var inStream = BytesToStream(xlsx);
        using var outStream = new MemoryStream();
        svc.ConvertXlsxToCsv(inStream, outStream);
        outStream.Position = 0;
        using var reader = new StreamReader(outStream, Encoding.UTF8);
        var text = reader.ReadToEnd();
        // Header line should contain Name, Age etc
        Assert.Contains("Name", text);
        // The name field should be quoted and inner quotes doubled
        var expected = "\"Smith, \"\"John\"\"\"";
        Assert.Contains(expected, text);
    }

    [Fact]
    public async Task ExportAndParseAsync_RoundTrip()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 3, Name = "Carol", Age = 33 }];
        var bytes = await svc.ExportToXlsxBytesAsync(data, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(bytes);
        using var ms = BytesToStream(bytes);
        var dict = await svc.ParseXlsxStreamAsDictionaryAsync(ms, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Single(dict);
        var row = dict[0].Values.ToList();
        Assert.Contains("Carol", row);
        Assert.Contains("33", row);
    }

    [Fact]
    public void ExportToXlsxBytes_RoundTrip_NoSelectedProperties()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 10, Name = "Derek", Age = 45 }];
        var bytes = svc.ExportToXlsxBytes(data);
        using var ms = BytesToStream(bytes);
        var dict = svc.ParseXlsxStreamAsDictionary(ms);
        Assert.Single(dict);
        // When exporting without selectedProperties the first column should be Id
        Assert.Equal("10", dict[0][0]);
        Assert.Equal("Derek", dict[0][1]);
    }

    [Fact]
    public void ExportSelectedProperties_ToXlsxStream_And_Parse()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 11, Name = "Eve", Age = 29 }];
        List<PropertyInfo> props = [typeof(TestModel).GetProperty(nameof(TestModel.Name))!, typeof(TestModel).GetProperty(nameof(TestModel.Age))!];
        using var ms = new MemoryStream();
        svc.ExportToXlsx(data, props, ms);
        ms.Position = 0;
        var dict = svc.ParseXlsxStreamAsDictionary(ms);
        Assert.Single(dict);
        Assert.Equal("Eve", dict[0][0]);
        Assert.Equal("29", dict[0][1]);
    }

    [Fact]
    public void ExportDateAndNumberTypes_PreservedAsParsableValues()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var model = new DateNumberModel {
            Date = new(2020, 1, 2),
            DecimalValue = 12.34m,
            DoubleValue = 2.5,
            Flag = true
        };

        var bytes = svc.ExportToXlsxBytes([model]);
        using var ms = BytesToStream(bytes);
        var dict = svc.ParseXlsxStreamAsDictionary(ms);
        Assert.Single(dict);
        var values = dict[0].Values.Select(v => v.ToString()).ToList();

        // At least one value should parse as DateTime and equal our date's date component
        Assert.Contains(values, s => DateTime.TryParse(s, out var d) && d.Date == model.Date.Date);
        // Numeric values should parse as doubles/decimals
        Assert.Contains(values, s => double.TryParse(s, out var dv) && Math.Abs(dv - (double)model.DecimalValue) < 0.0001);
        Assert.Contains(values, s => double.TryParse(s, out var dv2) && Math.Abs(dv2 - model.DoubleValue) < 0.0001);
        // Boolean should parse as bool or appear as True/False
        Assert.Contains(values, s => bool.TryParse(s, out var b) && b == model.Flag);
    }

    [Fact]
    public async Task ConvertXlsxToCsvAsync_ProducesQuotedFieldsForCommasAndQuotes()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "Smith, \"John\"", Age = 40 }];
        var xlsx = await svc.ExportToXlsxBytesAsync(data, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var inStream = BytesToStream(xlsx);
        using var outStream = new MemoryStream();
        await svc.ConvertXlsxToCsvAsync(inStream, outStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        outStream.Position = 0;
        using var reader = new StreamReader(outStream, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Contains("Name", text);
        Assert.Contains("\"Smith, \"\"John\"\"\"", text);
    }

    [Fact]
    public void ExportToXlsx_WithWorksheetName_UsesName()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 21, Name = "Named", Age = 50 }];
        var bytes = svc.ExportToXlsxBytes(data, "MySheet");
        using var ms = BytesToStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(ms);
        var config = new ExcelDataSetConfiguration();
        config.ConfigureDataTable = _ => new() { UseHeaderRow = true };
        var ds = reader.AsDataSet(config);
        Assert.Single(ds.Tables);
        Assert.Equal("MySheet", ds.Tables[0].TableName);
    }

    [Fact]
    public async Task ExportMultiSheetAsync_Bytes()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] a = [new() { Id = 31, Name = "AA", Age = 1 }];
        TestModel[] b = [new() { Id = 32, Name = "BB", Age = 2 }];
        var dataSets = new Dictionary<string, IEnumerable<TestModel>> { { "S1", a }, { "S2", b } };
        var bytes = await svc.ExportToXlsxBytesAsync(dataSets, TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = BytesToStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(ms);
        var config = new ExcelDataSetConfiguration();
        config.ConfigureDataTable = _ => new() { UseHeaderRow = true };
        var ds = reader.AsDataSet(config);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal("S1", ds.Tables[0].TableName);
        Assert.Equal("S2", ds.Tables[1].TableName);
    }

    [Fact]
    public void ExportEmptyData_ProducesWorkbookWithNoRows()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var empty = Array.Empty<TestModel>();
        var bytes = svc.ExportToXlsxBytes(empty);
        using var ms = BytesToStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(ms);
        var config = new ExcelDataSetConfiguration();
        config.ConfigureDataTable = _ => new() { UseHeaderRow = true };
        var ds = reader.AsDataSet(config);
        // Workbook should contain a sheet but no data rows
        Assert.Single(ds.Tables);
        Assert.Equal(0, ds.Tables[0].Rows.Count);
    }

    [Fact]
    public void ExportToXlsx_SaveToFile_RoundTrip()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 41, Name = "FileTest", Age = 60 }];
        var path = _tempSession.GetFilePath();
        svc.ExportToXlsx(data, path);
        var dict = svc.ParseXlsxFileAsDictionary(path);
        Assert.Single(dict);
        Assert.Contains("FileTest", dict[0].Values);
    }

    [Fact]
    public void ExportMultiSheet_ToStream_ProducesMultipleTables()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] a = [new() { Id = 51, Name = "X", Age = 5 }];
        TestModel[] b = [new() { Id = 52, Name = "Y", Age = 6 }];
        var dataSets = new Dictionary<string, IEnumerable<TestModel>> { { "One", a }, { "Two", b } };
        using var ms = new MemoryStream();
        svc.ExportToXlsx(dataSets, ms);
        ms.Position = 0;
        using var reader = ExcelReaderFactory.CreateReader(ms);
        var config = new ExcelDataSetConfiguration();
        config.ConfigureDataTable = _ => new() { UseHeaderRow = true };
        var ds = reader.AsDataSet(config);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal("One", ds.Tables[0].TableName);
        Assert.Equal("Two", ds.Tables[1].TableName);
    }

    [Fact]
    public void ExportWithUseHeaderRowFalse_ShowsHeaderAsData()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        // Configure service to not treat first row as header
        svc.SetExcelDataTableConfiguration(new() { UseHeaderRow = false });
        TestModel[] data = [new() { Id = 61, Name = "HeaderTest", Age = 7 }];
        var bytes = svc.ExportToXlsxBytes(data);
        using var ms = BytesToStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(ms);
        var config = new ExcelDataSetConfiguration();
        config.ConfigureDataTable = _ => new() { UseHeaderRow = false };
        var ds = reader.AsDataSet(config);
        // With UseHeaderRow=false the first row should contain header names (property names) as data
        Assert.Single(ds.Tables);
        var firstRow = ds.Tables[0].Rows[0];

        // Since headers are written as the first row in the service when UseHeaderRow is true, with false they become part of data in ExcelReader
        Assert.Contains("Id", firstRow.ItemArray.Select(o => o?.ToString()));
    }

    [Fact]
    public void ExportSelectedProperties_OrderPreserved()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 71, Name = "Order", Age = 77 }];
        var props = new List<PropertyInfo> { typeof(TestModel).GetProperty(nameof(TestModel.Age))!, typeof(TestModel).GetProperty(nameof(TestModel.Name))! };
        var bytes = svc.ExportToXlsxBytes(data, props);
        using var ms = BytesToStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(ms);
        var config = new ExcelDataSetConfiguration();
        config.ConfigureDataTable = _ => new() { UseHeaderRow = true };
        var ds = reader.AsDataSet(config);
        Assert.Single(ds.Tables);
        // First data row: Age then Name
        var valueRow = ds.Tables[0].Rows[0];
        Assert.Equal("77", valueRow[0].ToString());
        Assert.Equal("Order", valueRow[1].ToString());
    }

    [Fact]
    public void ExportLargeDataset_SmokeTest()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var list = Enumerable.Range(1, 1000).Select(i => new TestModel { Id = i, Name = "N" + i, Age = i % 100 }).ToArray();
        var bytes = svc.ExportToXlsxBytes(list);
        using var ms = BytesToStream(bytes);
        var dict = svc.ParseXlsxStreamAsDictionary(ms);
        Assert.Equal(1000, dict.Count);
    }

    [Fact]
    public void ExportToXlsxFromDictionary_WithHeaderRow_RoundTrips()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = new Dictionary<int, IReadOnlyDictionary<int, string>> {
            [0] = new Dictionary<int, string> { [0] = "H1", [1] = "H2", [2] = "H3" },
            [1] = new Dictionary<int, string> { [0] = "a", [1] = "b", [2] = "c" },
            [2] = new Dictionary<int, string> { [0] = "x", [1] = "y", [2] = "z" }
        };

        var bytes = svc.ExportToXlsxBytesFromDictionary(data);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        using var ms = BytesToStream(bytes);
        var dict = svc.ParseXlsxStreamAsDictionary(ms);
        Assert.Equal(2, dict.Count); // Header row becomes header, 2 data rows
        Assert.Equal("a", dict[0][0]);
        Assert.Equal("x", dict[1][0]);
    }

    [Fact]
    public void ExportToXlsxFromDictionary_WithoutHeaderRow_RoundTrips()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = new Dictionary<int, IReadOnlyDictionary<int, string>> {
            [0] = new Dictionary<int, string> { [0] = "r1c1", [1] = "r1c2" }, [1] = new Dictionary<int, string> { [0] = "r2c1", [1] = "r2c2" }
        };

        var bytes = svc.ExportToXlsxBytesFromDictionary(data, false);
        using var ms = BytesToStream(bytes);
        var dict = svc.ParseXlsxStreamAsDictionary(ms);
        Assert.Equal(2, dict.Count);
        Assert.Contains("r1c1", dict[0].Values);
        Assert.Contains("r2c1", dict[1].Values);
    }

    [Fact]
    public void ExportToXlsxFromDictionary_ToFile_RoundTrips()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = new Dictionary<int, IReadOnlyDictionary<int, string>> {
            [0] = new Dictionary<int, string> { [0] = "Col1", [1] = "Col2" }, [1] = new Dictionary<int, string> { [0] = "v1", [1] = "v2" }
        };

        var path = _tempSession.GetFilePath("dict-export.xlsx");
        svc.ExportToXlsxFromDictionary(data, path);
        Assert.True(File.Exists(path));
        var dict = svc.ParseXlsxFileAsDictionary(path);
        Assert.Single(dict);
        Assert.Equal("v1", dict[0][0]);
    }

    [Fact]
    public void ExportToXlsxFromDataTable_RoundTrips()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var dt = new DataTable.Models.DataTable();
        dt.SetHeader(0, "A").SetHeader(1, "B").SetHeader(2, "C");
        dt.AddRow().SetCell(0, "1").SetCell(1, "2").SetCell(2, "3");
        dt.AddRow().SetCell(0, "4").SetCell(1, "5").SetCell(2, "6");
        var bytes = svc.ExportToXlsxBytesFromDataTable(dt);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        var result = svc.ParseXlsxBytesAsDataTable(bytes);
        Assert.True(result.IsSuccess);
        var parsed = result.ValueOrThrow();
        Assert.Equal(3, parsed.Headers.Count);
        Assert.Equal(2, parsed.Rows.Count);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("6", parsed.Rows[1][2].DisplayValue);
    }

    [Fact]
    public void ExportToXlsxFromDataTable_ToFile_RoundTrips()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var dt = new DataTable.Models.DataTable();
        dt.SetHeader(0, "X").SetHeader(1, "Y");
        dt.AddRow().SetCell(0, "10").SetCell(1, "20");
        var path = _tempSession.GetFilePath("dt-export.xlsx");
        svc.ExportToXlsxFromDataTable(dt, path);
        Assert.True(File.Exists(path));
        var result = svc.ParseXlsxFileAsDataTable(path);
        Assert.True(result.IsSuccess);
        Assert.Equal("10", result.ValueOrThrow().Rows[0][0].DisplayValue);
    }

    [Fact]
    public void ConvertXlsxToCsv_FileToFile_ProducesCsv()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "CsvFile", Age = 25 }];
        var xlsxPath = _tempSession.GetFilePath("source.xlsx");
        var csvPath = _tempSession.GetFilePath("output.csv");
        File.WriteAllBytes(xlsxPath, svc.ExportToXlsxBytes(data));
        svc.ConvertXlsxToCsv(xlsxPath, csvPath);
        Assert.True(File.Exists(csvPath));
        var csvText = File.ReadAllText(csvPath);
        Assert.Contains("Name", csvText);
        Assert.Contains("CsvFile", csvText);
    }

    [Fact]
    public void ConvertXlsxToCsvBytes_ReturnsCsvBytes()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 99, Name = "BytesTest", Age = 42 }];
        var xlsxBytes = svc.ExportToXlsxBytes(data);
        var csvBytes = svc.ConvertXlsxToCsvBytes(xlsxBytes);
        Assert.NotNull(csvBytes);
        Assert.True(csvBytes.Length > 0);
        var text = Encoding.UTF8.GetString(csvBytes);
        Assert.Contains("BytesTest", text);
    }

    [Fact]
    public void ConvertXlsxToCsvBytes_FromStream_ReturnsCsvBytes()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "Stream", Age = 10 }];
        var xlsxBytes = svc.ExportToXlsxBytes(data);
        using var inputStream = BytesToStream(xlsxBytes);
        var csvBytes = svc.ConvertXlsxToCsvBytes(inputStream);
        Assert.NotNull(csvBytes);
        Assert.Contains("Stream", Encoding.UTF8.GetString(csvBytes));
    }

    [Fact]
    public void ParseXlsxStreamAsDataTable_WithUseHeaderRowFalse_TreatsFirstRowAsData()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data = [new() { Id = 1, Name = "NoHeader", Age = 33 }];
        var bytes = svc.ExportToXlsxBytes(data);
        using var ms = BytesToStream(bytes);
        var result = svc.ParseXlsxStreamAsDataTable(ms, false);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.True(dt.Headers.Count >= 0);
        Assert.True(dt.Rows.Count >= 1);
    }

    [Fact]
    public async Task ExportToXlsxFromDataTableAsync_RoundTrips()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var dt = new DataTable.Models.DataTable();
        dt.SetHeader(0, "Async").SetHeader(1, "Test");
        dt.AddRow().SetCell(0, "val1").SetCell(1, "val2");
        var bytes = await svc.ExportToXlsxBytesFromDataTableAsync(dt, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(bytes);
        var result = await svc.ParseXlsxBytesAsDataTableAsync(bytes, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.Equal("val1", result.ValueOrThrow().Rows[0][0].DisplayValue);
    }

    [Fact]
    public async Task BatchParseFilesAsDataTableAsync_ReturnsResultPerFile()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        TestModel[] data1 = [new() { Id = 1, Name = "AsyncA", Age = 1 }];
        TestModel[] data2 = [new() { Id = 2, Name = "AsyncB", Age = 2 }];
        var path1 = _tempSession.GetFilePath("async-batch1.xlsx");
        var path2 = _tempSession.GetFilePath("async-batch2.xlsx");
        File.WriteAllBytes(path1, svc.ExportToXlsxBytes(data1));
        File.WriteAllBytes(path2, svc.ExportToXlsxBytes(data2));
        var results = await svc.BatchParseFilesAsDataTableAsync([path1, path2], ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.True(results[1].IsSuccess);
        Assert.Equal("AsyncA", results[0].ValueOrThrow().Rows[0][1].DisplayValue);
        Assert.Equal("AsyncB", results[1].ValueOrThrow().Rows[0][1].DisplayValue);
    }

#if !NETSTANDARD2_0
    [Fact]
    public async Task ExportToXlsxAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = Enumerable.Range(1, 10000).Select(i => new TestModel { Id = i, Name = "N" + i, Age = i }).ToArray();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false); // Cancel immediately
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await svc.ExportToXlsxAsync(data, _tempSession.GetFilePath(), ct: cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ExportToXlsxAsync_WithCancellationDuringProcessing_ThrowsOperationCanceledException()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = Enumerable.Range(1, 50000).Select(i => new TestModel { Id = i, Name = "N" + i, Age = i }).ToArray();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)); // Cancel after 10ms
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
            await svc.ExportToXlsxAsync(data, _tempSession.GetFilePath(), ct: cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ExportToXlsxBytesAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = Enumerable.Range(1, 10000).Select(i => new TestModel { Id = i, Name = "N" + i, Age = i }).ToArray();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await svc.ExportToXlsxBytesAsync(data, ct: cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ExportMultiSheetAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var dataSets = new Dictionary<string, IEnumerable<TestModel>> {
            { "Sheet1", Enumerable.Range(1, 10000).Select(i => new TestModel { Id = i, Name = "N" + i, Age = i }) },
            { "Sheet2", Enumerable.Range(1, 10000).Select(i => new TestModel { Id = i, Name = "N" + i, Age = i }) }
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await svc.ExportToXlsxAsync(dataSets, _tempSession.GetFilePath(), cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ParseXlsxStreamAsDictionaryAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = new[] { new TestModel { Id = 1, Name = "Test", Age = 30 } };
        var bytes = await svc.ExportToXlsxBytesAsync(data, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = BytesToStream(bytes);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await svc.ParseXlsxStreamAsDictionaryAsync(ms, cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ConvertXlsxToCsvAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = new[] { new TestModel { Id = 1, Name = "Test", Age = 30 } };
        var xlsxBytes = await svc.ExportToXlsxBytesAsync(data, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var inputStream = BytesToStream(xlsxBytes);
        using var outputStream = new MemoryStream();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await svc.ConvertXlsxToCsvAsync(inputStream, outputStream, ct: cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ExportToXlsxAsync_WithInvalidPath_ThrowsException()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = new[] { new TestModel { Id = 1, Name = "Test", Age = 30 } };
        // Try to write to a path where the parent is a file (not a directory) - this will fail
        var existingFile = await _tempSession.CreateFileAsync(""u8.ToArray(), Guid.NewGuid() + ".tmp", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var invalidPath = Path.Combine(existingFile, "file.xlsx");
        await Assert.ThrowsAnyAsync<Exception>(async () => {
            await svc.ExportToXlsxAsync(data, invalidPath, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ExportToXlsxAsync_WithNullData_HandlesGracefully()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        IEnumerable<TestModel>? nullData = null;

        // Should handle null gracefully (may throw ArgumentNullException or handle it)
        await Assert.ThrowsAnyAsync<Exception>(async () => {
            await svc.ExportToXlsxAsync(nullData!, _tempSession.GetFilePath(), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ExportToXlsxAsync_WithEmptyData_ProducesValidFile()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var emptyData = Array.Empty<TestModel>();
        var path = _tempSession.GetFilePath();
        await svc.ExportToXlsxAsync(emptyData, path, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
    }

    [Fact]
    public async Task ExportToXlsxAsync_WithSelectedProperties_WorksCorrectly()
    {
        EnsureCodePages();
        var svc = new XlsxService(_logger);
        var data = new[] { new TestModel { Id = 1, Name = "Test", Age = 30 } };
        var props = new List<PropertyInfo> { typeof(TestModel).GetProperty(nameof(TestModel.Name))! };
        var path = _tempSession.GetFilePath();
        await svc.ExportToXlsxAsync(data, props, path, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(File.Exists(path));

        // Verify content
        var dict = await svc.ParseXlsxFileAsDictionaryAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Single(dict);
        Assert.Equal("Test", dict[0][0]);
    }
#endif
}