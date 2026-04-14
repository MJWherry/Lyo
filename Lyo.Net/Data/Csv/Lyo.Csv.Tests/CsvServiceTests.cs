using System.Globalization;
using System.Reflection;
using System.Text;
using CsvHelper;
using Lyo.Csv.Converters;
using Lyo.Csv.Models;
using Lyo.Csv.Tests.TestModels;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.Csv.Tests;

public class CsvServiceTests : IDisposable, IAsyncDisposable
{
    private readonly ILogger<CsvService> _logger;

    private readonly IIOTempSession _tempSession;

    public CsvServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<CsvService>();
        _tempSession = new IOTempSession(new(){ FileExtension = ".csv" }, loggerFactory.CreateLogger<IOTempSession>());
    }

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

    private static MemoryStream StringToStream(string s = "") => new(Encoding.UTF8.GetBytes(s));

    private static (string header, string[] rows) ReadHeaderAndRowsFromText(string text)
    {
        using var reader = new StringReader(text);
        var header = reader.ReadLine() ?? string.Empty;
        if (!string.IsNullOrEmpty(header) && header[0] == '\uFEFF')
            header = header.TrimStart('\uFEFF');

        var rows = new List<string>();
        while (reader.ReadLine() is { } line)
            rows.Add(line);

        return (header, rows.ToArray());
    }

    private static string GetHeaderFromBytes(byte[] bytes) => ReadHeaderAndRowsFromText(Encoding.UTF8.GetString(bytes)).header;

    private static string[] GetRowsFromBytes(byte[] bytes) => ReadHeaderAndRowsFromText(Encoding.UTF8.GetString(bytes)).rows;

    [Fact]
    public void ExportToCsvBytes_And_ParseStream_RoundTrip()
    {
        var svc = new CsvService(_logger);
        List<Person> data = [new() { Id = 1, Name = "Alice" }, new() { Id = 2, Name = "Bob" }];
        var bytes = svc.ExportToCsvBytes(data);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        using var ms = new MemoryStream(bytes);
        var parsed = svc.ParseStream<Person>(ms).ToList();
        Assert.Equal(2, parsed.Count);
        Assert.Equal(1, parsed[0].Id);
        Assert.Equal("Alice", parsed[0].Name);
        Assert.Equal(2, parsed[1].Id);
        Assert.Equal("Bob", parsed[1].Name);
    }

    [Fact]
    public void ExportSelectedProperties_WritesOnlyThoseHeadersAndValues()
    {
        var svc = new CsvService(_logger);
        List<Person> data = [new() { Id = 10, Name = "Charlie", Age = 30 }];
        List<PropertyInfo> props = [typeof(Person).GetProperty(nameof(Person.Name))!, typeof(Person).GetProperty(nameof(Person.Age))!];
        var bytes = svc.ExportToCsvBytes(data, props);
        var header = GetHeaderFromBytes(bytes);
        var rows = GetRowsFromBytes(bytes);
        Assert.Equal("Name,Age", header);
        Assert.NotEmpty(rows);
        Assert.Equal("Charlie,30", rows[0]);
    }

    [Fact]
    public void ParseStreamAsDictionary_IncludesHeaderRowAndValues()
    {
        var csv = "ColA,ColB\nval1,val2\nval3,val4\n";
        var svc = new CsvService(_logger);
        using var ms = StringToStream(csv);
        var dict = svc.ParseStreamAsDictionary(ms);
        Assert.Equal(3, dict.Count);
        Assert.Equal("ColA", dict[0][0]);
        Assert.Equal("ColB", dict[0][1]);
        Assert.Equal("val1", dict[1][0]);
        Assert.Equal("val2", dict[1][1]);
        Assert.Equal("val3", dict[2][0]);
        Assert.Equal("val4", dict[2][1]);
    }

    [Fact]
    public void ParseStreamAsDataTable_WithHeader_SplitsHeadersAndRows()
    {
        var csv = "ColA,ColB\nval1,val2\nval3,val4\n";
        var svc = new CsvService(_logger);
        using var ms = StringToStream(csv);
        var result = svc.ParseStreamAsDataTable(ms);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal(2, dt.Headers.Count);
        Assert.Equal("ColA", dt.Headers[0].DisplayValue);
        Assert.Equal("ColB", dt.Headers[1].DisplayValue);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("val1", dt.Rows[0][0].DisplayValue);
        Assert.Equal("val2", dt.Rows[0][1].DisplayValue);
        Assert.Equal("val3", dt.Rows[1][0].DisplayValue);
        Assert.Equal("val4", dt.Rows[1][1].DisplayValue);
    }

    [Fact]
    public void ParseStreamAsDataTable_WithoutHeader_UsesColumnN()
    {
        var svc = new CsvService(_logger);
        svc.SetCsvConfiguration(new(CultureInfo.InvariantCulture) { HasHeaderRecord = false });
        var csv = "a,b\n1,2\n";
        using var ms = StringToStream(csv);
        var result = svc.ParseStreamAsDataTable(ms);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal(2, dt.Headers.Count);
        Assert.Equal("Column0", dt.Headers[0].DisplayValue);
        Assert.Equal("Column1", dt.Headers[1].DisplayValue);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("a", dt.Rows[0][0].DisplayValue);
        Assert.Equal("b", dt.Rows[0][1].DisplayValue);
        Assert.Equal("1", dt.Rows[1][0].DisplayValue);
        Assert.Equal("2", dt.Rows[1][1].DisplayValue);
    }

    [Fact]
    public void ParseStreamAsDataTable_WithHasHeaderRowFalse_OverridesConfig()
    {
        var csv = "a,b\n1,2\n";
        var svc = new CsvService(_logger);
        using var ms = StringToStream(csv);
        var result = svc.ParseStreamAsDataTable(ms, false);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal("Column0", dt.Headers[0].DisplayValue);
        Assert.Equal("Column1", dt.Headers[1].DisplayValue);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("a", dt.Rows[0][0].DisplayValue);
        Assert.Equal("1", dt.Rows[1][0].DisplayValue);
    }

    [Fact]
    public void ParseBytesAsDataTable_ReturnsDataTable()
    {
        var csv = "X,Y\n10,20\n30,40\n";
        var svc = new CsvService(_logger);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var result = svc.ParseBytesAsDataTable(bytes);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal(2, dt.Headers.Count);
        Assert.Equal("X", dt.Headers[0].DisplayValue);
        Assert.Equal("Y", dt.Headers[1].DisplayValue);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("10", dt.Rows[0][0].DisplayValue);
        Assert.Equal("20", dt.Rows[0][1].DisplayValue);
        Assert.Equal("30", dt.Rows[1][0].DisplayValue);
        Assert.Equal("40", dt.Rows[1][1].DisplayValue);
    }

    [Fact]
    public void ParseFileAsDataTable_ReturnsDataTable()
    {
        var svc = new CsvService(_logger);
        var csv = "A,B,C\n1,2,3\n4,5,6\n";
        var path = _tempSession.GetFilePath("parse-datatable.csv");
        File.WriteAllText(path, csv);
        var result = svc.ParseFileAsDataTable(path);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.Equal(3, dt.Headers.Count);
        Assert.Equal("A", dt.Headers[0].DisplayValue);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("1", dt.Rows[0][0].DisplayValue);
        Assert.Equal("6", dt.Rows[1][2].DisplayValue);
    }

    [Fact]
    public void ExportToHtmlTable_ProducesValidHtmlWithTable()
    {
        var csv = "Col1,Col2\nfoo,bar\nbaz,qux\n";
        var svc = new CsvService(_logger);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var html = svc.ExportToHtmlTable(bytes);
        Assert.NotNull(html);
        Assert.Contains("<!DOCTYPE", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<table", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Col1", html);
        Assert.Contains("Col2", html);
        Assert.Contains("foo", html);
        Assert.Contains("bar", html);
    }

    [Fact]
    public void BatchParseFilesAsDataTable_ReturnsResultPerFile()
    {
        var svc = new CsvService(_logger);
        var path1 = _tempSession.GetFilePath("batch1.csv");
        var path2 = _tempSession.GetFilePath("batch2.csv");
        File.WriteAllText(path1, "H1,H2\n1,2\n");
        File.WriteAllText(path2, "A,B,C\nx,y,z\n");
        var results = svc.BatchParseFilesAsDataTable([path1, path2]);
        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        var dt1 = results[0].ValueOrThrow();
        Assert.Equal(2, dt1.Headers.Count);
        Assert.Single(dt1.Rows);
        Assert.True(results[1].IsSuccess);
        var dt2 = results[1].ValueOrThrow();
        Assert.Equal(3, dt2.Headers.Count);
        Assert.Equal("x", dt2.Rows[0][0].DisplayValue);
    }

    [Fact]
    public void RegisterClassMap_AllowsParsingWithCustomHeaderNames()
    {
        var csv = "Full Name\nEve\n";
        var svc = new CsvService(_logger);
        svc.RegisterClassMap<PersonNameMap>();
        using var ms = StringToStream(csv);
        var parsed = svc.ParseStream<PersonName>(ms).ToList();
        Assert.Single(parsed);
        Assert.Equal("Eve", parsed[0].Name);
    }

    [Fact]
    public void ExportWithSpecialCharacters_RoundTrip()
    {
        var svc = new CsvService(_logger);
        var special = new Person { Id = 5, Name = "Smith, \"John\"\nNewLine" };
        Person[] data = [special];
        var bytes = svc.ExportToCsvBytes(data);
        using var ms = new MemoryStream(bytes);
        var parsed = svc.ParseStream<Person>(ms).ToList();
        Assert.Single(parsed);
        Assert.Equal(special.Id, parsed[0].Id);
        Assert.Equal(special.Name, parsed[0].Name);
    }

    [Fact]
    public void ParseStreamAsDictionary_HandlesEmptyFieldsAsEmptyStrings()
    {
        var csv = "A,B\n1,\n,2\n";
        var svc = new CsvService(_logger);
        using var ms = StringToStream(csv);
        var dict = svc.ParseStreamAsDictionary(ms);
        Assert.Equal(3, dict.Count);
        Assert.Equal("1", dict[1][0]);
        Assert.Equal(string.Empty, dict[1][1]);
        Assert.Equal(string.Empty, dict[2][0]);
        Assert.Equal("2", dict[2][1]);
    }

    [Fact]
    public void RegisterClassMap_ChangesHeaderOnExport()
    {
        var svc = new CsvService(_logger);
        svc.RegisterClassMap<PersonNameMap>();
        PersonName[] data = [new() { Name = "Dana" }];
        var bytes = svc.ExportToCsvBytes(data);
        var header = GetHeaderFromBytes(bytes);
        var rows = GetRowsFromBytes(bytes);
        Assert.Equal("Full Name", header);
        Assert.NotEmpty(rows);
        Assert.Equal("Dana", rows[0]);
    }

    [Fact]
    public void ExportToCsv_Stream_WritesAndCanBeParsed()
    {
        var svc = new CsvService(_logger);
        Person[] data = [new() { Id = 7, Name = "Gail" }];
        using var ms = new MemoryStream();
        svc.ExportToCsvStream(data, ms);
        ms.Position = 0;
        var parsed = svc.ParseStream<Person>(ms).ToList();
        Assert.Single(parsed);
        Assert.Equal(7, parsed[0].Id);
        Assert.Equal("Gail", parsed[0].Name);
    }

    [Fact]
    public async Task ExportAndParseAsync_RoundTrip()
    {
        var svc = new CsvService(_logger);
        Person[] data = [new() { Id = 8, Name = "Hank" }];
        var bytes = await svc.ExportToCsvBytesAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes);
        var parsed = await svc.ParseStreamAsync<Person>(ms, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Single(parsed);
        Assert.Equal(8, parsed[0].Id);
        Assert.Equal("Hank", parsed[0].Name);
    }

    [Fact]
    public async Task ExportSelectedPropertiesAsync_WritesHeadersAndValues()
    {
        var svc = new CsvService(_logger);
        Person[] data = [new() { Id = 20, Name = "Ivy", Age = 40 }];
        List<PropertyInfo> props = [typeof(Person).GetProperty(nameof(Person.Name))!, typeof(Person).GetProperty(nameof(Person.Age))!];
        var bytes = await svc.ExportToCsvBytesAsync(data, props, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var header = GetHeaderFromBytes(bytes);
        var rows = GetRowsFromBytes(bytes);
        Assert.Equal("Name,Age", header);
        Assert.NotEmpty(rows);
        Assert.Equal("Ivy,40", rows[0]);
    }

    [Fact]
    public void Int32Converter_WorksBothWays()
    {
        var conv = new Int32CsvConverter();
        var from = conv.ConvertFromString("123", null!, null!);
        Assert.IsType<int>(from);
        Assert.Equal(123, (int)from);
        Assert.Null(conv.ConvertFromString("", null!, null!));
        Assert.Equal("123", conv.ConvertToString(123, null!, null!));
        Assert.Equal(string.Empty, conv.ConvertToString(null, null!, null!));
    }

    [Fact]
    public void Int64Converter_WorksBothWays()
    {
        var conv = new Int64CsvConverter();
        var from = conv.ConvertFromString("9000000000", null!, null!);
        Assert.IsType<long>(from);
        Assert.Equal(9000000000L, (long)from);
        Assert.Null(conv.ConvertFromString("", null!, null!));
        Assert.Equal("9000000000", conv.ConvertToString(9000000000L, null!, null!));
        Assert.Equal(string.Empty, conv.ConvertToString(null, null!, null!));
    }

    [Fact]
    public void DecimalConverter_WorksBothWays_WithInvariantCulture()
    {
        var conv = new DecimalCsvConverter();
        var prev = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var from = conv.ConvertFromString("12.34", null!, null!);
            Assert.IsType<decimal>(from);
            Assert.Equal(12.34m, (decimal)from);
            Assert.Null(conv.ConvertFromString("", null!, null!));
            Assert.Equal("12.34", conv.ConvertToString(12.34m, null!, null!));
            Assert.Equal(string.Empty, conv.ConvertToString(null, null!, null!));
        }
        finally {
            CultureInfo.CurrentCulture = prev;
        }
    }

    [Fact]
    public void YesNoBoolConverter_InterpretsValuesCorrectly()
    {
        var conv = new YesNoBoolCsvConverter();
        var t = conv.ConvertFromString("yes", null!, null!);
        Assert.IsType<bool>(t);
        Assert.True((bool)t);
        var f = conv.ConvertFromString("no", null!, null!);
        Assert.IsType<bool>(f);
        Assert.False((bool)f);
        Assert.Null(conv.ConvertFromString("", null!, null!));
        Assert.Equal("yes", conv.ConvertToString(true, null!, null!));
        Assert.Equal("no", conv.ConvertToString(false, null!, null!));
        Assert.Equal("yes", conv.ConvertToString(1, null!, null!));
        Assert.Equal("no", conv.ConvertToString(0, null!, null!));
        Assert.Null(conv.ConvertToString(2, null!, null!));
        Assert.Null(conv.ConvertToString(null, null!, null!));
    }

    [Fact]
    public void BadDataFound_IsLogged_WhenColumnCountChanges()
    {
        // Header has two columns, second row has three -> CsvHelper will throw because DetectColumnCountChanges=true
        var csv = "A,B\n1,2,3\n";
        using var ms = StringToStream(csv);
        var ex = Assert.Throws<BadDataException>(() => new CsvService(_logger).ParseStreamAsDictionary(ms));
        Assert.Contains("inconsistent number of columns", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public void ExportToCsvBytes_UsesSpecifiedEncoding(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var svc = new CsvService(_logger);
        svc.SetEncoding(encoding);
        var data = new List<Person> { new() { Id = 1, Name = "Test" } };
        var bytes = svc.ExportToCsvBytes(data);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // Verify the encoding by reading back
        var decoded = encoding.GetString(bytes);
        Assert.Contains("Id", decoded);
        Assert.Contains("Name", decoded);
        Assert.Contains("Test", decoded);
    }

    [Fact]
    public void ExportToCsvBytes_WithUTF8BOM_IncludesBOM()
    {
        var svc = new CsvService(_logger);
        svc.SetEncoding(new UTF8Encoding(true));
        var data = new List<Person> { new() { Id = 1, Name = "Test" } };
        var bytes = svc.ExportToCsvBytes(data);

        // UTF-8 BOM is EF BB BF
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public void ExportToCsvBytes_WithUTF16_HandlesUnicodeCharacters()
    {
        var svc = new CsvService(_logger);
        svc.SetEncoding(Encoding.Unicode); // UTF-16 LE
        var data = new List<Person> { new() { Id = 1, Name = "José" }, new() { Id = 2, Name = "François" }, new() { Id = 3, Name = "北京" } };
        var bytes = svc.ExportToCsvBytes(data);
        Assert.NotNull(bytes);

        // Round-trip test
        using var ms = new MemoryStream(bytes);
        var parsed = svc.ParseStream<Person>(ms).ToList();
        Assert.Equal(3, parsed.Count);
        Assert.Equal("José", parsed[0].Name);
        Assert.Equal("François", parsed[1].Name);
        Assert.Equal("北京", parsed[2].Name);
    }

    [Fact]
    public void ExportToCsvStream_UsesSpecifiedEncoding()
    {
        var encoding = Encoding.UTF8;
        var svc = new CsvService(_logger);
        svc.SetEncoding(encoding);
        var data = new List<Person> { new() { Id = 1, Name = "Stream Test" } };
        using var ms = new MemoryStream();
        svc.ExportToCsvStream(data, ms);
        ms.Position = 0;
        var bytes = ms.ToArray();
        var decoded = encoding.GetString(bytes);
        Assert.Contains("Id", decoded);
        Assert.Contains("Name", decoded);
        Assert.Contains("Stream Test", decoded);
    }

    [Fact]
    public void ExportToCsvString_UsesDefaultEncoding()
    {
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "String Test" } };
        var csvString = svc.ExportToCsvString(data);
        Assert.NotNull(csvString);
        Assert.Contains("Id", csvString);
        Assert.Contains("Name", csvString);
        Assert.Contains("String Test", csvString);
    }

    [Fact]
    public void ExportToCsvString_WithUnicodeCharacters_PreservesCharacters()
    {
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Café" }, new() { Id = 2, Name = "Müller" }, new() { Id = 3, Name = "東京" } };
        var csvString = svc.ExportToCsvString(data);
        Assert.Contains("Café", csvString);
        Assert.Contains("Müller", csvString);
        Assert.Contains("東京", csvString);
    }

    [Fact]
    public async Task ExportToCsvBytesAsync_UsesSpecifiedEncoding()
    {
        var encoding = Encoding.UTF8;
        var svc = new CsvService(_logger);
        svc.SetEncoding(encoding);
        var data = new List<Person> { new() { Id = 1, Name = "Async Test" } };
        var bytes = await svc.ExportToCsvBytesAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(bytes);
        var decoded = encoding.GetString(bytes);
        Assert.Contains("Async Test", decoded);
    }

    [Fact]
    public async Task ExportToCsvStreamAsync_UsesSpecifiedEncoding()
    {
        var encoding = Encoding.UTF8;
        var svc = new CsvService(_logger);
        svc.SetEncoding(encoding);
        var data = new List<Person> { new() { Id = 1, Name = "Async Stream Test" } };
        using var ms = new MemoryStream();
        await svc.ExportToCsvStreamAsync(data, ms, TestContext.Current.CancellationToken).ConfigureAwait(false);
        ms.Position = 0;
        var bytes = ms.ToArray();
        var decoded = encoding.GetString(bytes);
        Assert.Contains("Async Stream Test", decoded);
    }

    [Fact]
    public async Task ExportToCsvStringAsync_PreservesUnicodeCharacters()
    {
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Español" }, new() { Id = 2, Name = "Русский" } };
        var csvString = await svc.ExportToCsvStringAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Contains("Español", csvString);
        Assert.Contains("Русский", csvString);
    }

    [Fact]
    public void ParseStream_WithUTF16Encoding_ReadsCorrectly()
    {
        var svc = new CsvService(_logger);
        svc.SetEncoding(Encoding.Unicode);
        var data = new List<Person> { new() { Id = 1, Name = "UTF-16 Test" } };
        var bytes = svc.ExportToCsvBytes(data);
        using var ms = new MemoryStream(bytes);
        var parsed = svc.ParseStream<Person>(ms).ToList();
        Assert.Single(parsed);
        Assert.Equal("UTF-16 Test", parsed[0].Name);
    }

    [Fact]
    public async Task ParseStreamAsync_WithUTF16Encoding_ReadsCorrectly()
    {
        var svc = new CsvService(_logger);
        svc.SetEncoding(Encoding.Unicode);
        var data = new List<Person> { new() { Id = 1, Name = "UTF-16 Async Test" } };
        var bytes = await svc.ExportToCsvBytesAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes);
        var parsed = await svc.ParseStreamAsync<Person>(ms, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Single(parsed);
        Assert.Equal("UTF-16 Async Test", parsed[0].Name);
    }

    [Fact]
    public void ExportToCsvBytes_WithSelectedProperties_UsesSpecifiedEncoding()
    {
        var encoding = Encoding.UTF8;
        var svc = new CsvService(_logger);
        svc.SetEncoding(encoding);
        var data = new List<Person> { new() { Id = 1, Name = "Selected", Age = 25 } };
        var props = new List<PropertyInfo> { typeof(Person).GetProperty(nameof(Person.Name))!, typeof(Person).GetProperty(nameof(Person.Age))! };
        var bytes = svc.ExportToCsvBytes(data, props);
        var decoded = encoding.GetString(bytes);
        Assert.Contains("Name", decoded);
        Assert.Contains("Age", decoded);
        Assert.Contains("Selected", decoded);
        Assert.Contains("25", decoded);
    }

    [Fact]
    public async Task ExportToCsvBytesAsync_WithSelectedProperties_UsesSpecifiedEncoding()
    {
        var encoding = Encoding.UTF8;
        var svc = new CsvService(_logger);
        svc.SetEncoding(encoding);
        var data = new List<Person> { new() { Id = 1, Name = "Async Selected", Age = 30 } };
        var props = new List<PropertyInfo> { typeof(Person).GetProperty(nameof(Person.Name))!, typeof(Person).GetProperty(nameof(Person.Age))! };
        var bytes = await svc.ExportToCsvBytesAsync(data, props, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decoded = encoding.GetString(bytes);
        Assert.Contains("Async Selected", decoded);
        Assert.Contains("30", decoded);
    }

    [Fact]
    public void Encoding_RoundTrip_WithSpecialCharacters()
    {
        var svc = new CsvService(_logger);
        svc.SetEncoding(Encoding.UTF8);
        var specialChars = new List<Person> { new() { Id = 1, Name = "Test with émojis 🎉 and symbols ©®™" }, new() { Id = 2, Name = "日本語" }, new() { Id = 3, Name = "العربية" } };
        var bytes = svc.ExportToCsvBytes(specialChars);
        using var ms = new MemoryStream(bytes);
        var parsed = svc.ParseStream<Person>(ms).ToList();
        Assert.Equal(3, parsed.Count);
        Assert.Equal("Test with émojis 🎉 and symbols ©®™", parsed[0].Name);
        Assert.Equal("日本語", parsed[1].Name);
        Assert.Equal("العربية", parsed[2].Name);
    }

    [Fact]
    public void SetEncoding_ChangesEncodingForSubsequentExports()
    {
        var svc = new CsvService(_logger);

        // First export with UTF-8
        svc.SetEncoding(Encoding.UTF8);
        var data1 = new List<Person> { new() { Id = 1, Name = "UTF-8" } };
        var bytes1 = svc.ExportToCsvBytes(data1);

        // Change to UTF-16
        svc.SetEncoding(Encoding.Unicode);
        var data2 = new List<Person> { new() { Id = 2, Name = "UTF-16" } };
        var bytes2 = svc.ExportToCsvBytes(data2);

        // UTF-16 should produce different byte array (different size due to 2 bytes per char)
        Assert.NotEqual(bytes1.Length, bytes2.Length);

        // Verify both can be read back correctly
        using var ms1 = new MemoryStream(bytes1);
        var parsed1 = svc.ParseStream<Person>(ms1).ToList();
        Assert.Equal("UTF-8", parsed1[0].Name);
        using var ms2 = new MemoryStream(bytes2);
        var parsed2 = svc.ParseStream<Person>(ms2).ToList();
        Assert.Equal("UTF-16", parsed2[0].Name);
    }

    // Feature 1: Streaming parsing tests
    [Fact]
    public async Task ParseFileStreamingAsync_YieldsRecordsOneAtATime()
    {
        var tempFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Alice" }, new() { Id = 2, Name = "Bob" }, new() { Id = 3, Name = "Charlie" } };
        await svc.ExportToCsvAsync(data, tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var records = new List<Person>();
        await foreach (var record in svc.ParseFileStreamingAsync<Person>(tempFile, ct: TestContext.Current.CancellationToken).ConfigureAwait(false))
            records.Add(record);

        Assert.Equal(3, records.Count);
        Assert.Equal("Alice", records[0].Name);
        Assert.Equal("Bob", records[1].Name);
        Assert.Equal("Charlie", records[2].Name);
    }

    [Fact]
    public async Task ParseStreamStreamingAsync_ResetsStreamPosition()
    {
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Test" } };
        var bytes = await svc.ExportToCsvBytesAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes);
        ms.Position = bytes.Length; // Move to end
        var records = new List<Person>();
        await foreach (var record in svc.ParseStreamStreamingAsync<Person>(ms, ct: TestContext.Current.CancellationToken).ConfigureAwait(false))
            records.Add(record);

        Assert.Single(records);
        Assert.Equal("Test", records[0].Name);
    }

    [Fact]
    public async Task ParseStreamStreamingAsync_RespectsMaxRowsOption()
    {
        var svc = new CsvService(_logger);
        var data = Enumerable.Range(1, 10).Select(i => new Person { Id = i, Name = $"Person{i}" }).ToList();
        var bytes = await svc.ExportToCsvBytesAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes);
        var options = new CsvParseOptions { MaxRows = 5 };
        var records = new List<Person>();
        await foreach (var record in svc.ParseStreamStreamingAsync<Person>(ms, options, TestContext.Current.CancellationToken).ConfigureAwait(false))
            records.Add(record);

        Assert.Equal(5, records.Count);
    }

    [Fact]
    public async Task ExportToCsvWithProgressAsync_ReportsProgress()
    {
        var tempFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data = Enumerable.Range(1, 250).Select(i => new Person { Id = i, Name = $"Person{i}" }).ToList();
        var progressReports = new List<CsvProgress>();
        var progress = new SynchronousProgress<CsvProgress>(p => progressReports.Add(p));
        await svc.ExportToCsvWithProgressAsync(data, tempFile, progress, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(progressReports.Count > 0);
        Assert.Contains(progressReports, p => p.RowsProcessed > 0);
        Assert.Equal(250, progressReports.Last().TotalRows);
        Assert.Equal(250, progressReports.Last().RowsProcessed);
    }

    [Fact]
    public async Task ExportToCsvStreamWithProgressAsync_ReportsProgress()
    {
        var svc = new CsvService(_logger);
        var data = Enumerable.Range(1, 150).Select(i => new Person { Id = i, Name = $"Person{i}" }).ToList();
        var progressReports = new List<CsvProgress>();
        var progress = new SynchronousProgress<CsvProgress>(p => progressReports.Add(p));
        using var ms = new MemoryStream();
        await svc.ExportToCsvStreamWithProgressAsync(data, ms, progress, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(progressReports.Count > 0);
        Assert.Equal(150, progressReports.Last().TotalRows);
    }

    // Feature 3: Row-level error handling tests
    [Fact]
    public async Task ParseFileWithOptionsAsync_ContinuesOnError()
    {
        var tempFile = await _tempSession.CreateFileAsync("Id,Name,Age\n1,Alice,30\ninvalid,Bob,25\n3,Charlie,40", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var errors = new List<CsvParseError>();
        var options = new CsvParseOptions { ContinueOnError = true, OnError = error => errors.Add(error) };
        var records = await svc.ParseFileWithOptionsAsync<Person>(tempFile, options, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, records.Count); // Should parse 2 valid records
        Assert.Single(errors); // Should catch 1 error
        Assert.Equal("Alice", records[0].Name);
        Assert.Equal("Charlie", records[1].Name);
    }

    [Fact]
    public async Task ParseStreamWithOptionsAsync_AppliesRowFilter()
    {
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Alice", Age = 30 }, new() { Id = 2, Name = "Bob", Age = 25 }, new() { Id = 3, Name = "Charlie", Age = 40 } };
        var bytes = await svc.ExportToCsvBytesAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes);
        var options = new CsvParseOptions { RowFilter = row => int.TryParse(row["Age"], out var age) && age >= 30 };
        var records = await svc.ParseStreamWithOptionsAsync<Person>(ms, options, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, records.Count);
        Assert.Equal("Alice", records[0].Name);
        Assert.Equal("Charlie", records[1].Name);
    }

    // Feature 4: CSV statistics tests
    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectStatistics()
    {
        var tempFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Alice", Age = 30 }, new() { Id = 2, Name = "Bob", Age = 25 } };
        await svc.ExportToCsvAsync(data, tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var stats = await svc.GetStatisticsAsync(tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, stats.RowCount);
        Assert.Equal(3, stats.ColumnCount);
        Assert.Contains("Id", stats.Headers);
        Assert.Contains("Name", stats.Headers);
        Assert.Contains("Age", stats.Headers);
        Assert.True(stats.FileSizeBytes > 0);
        Assert.True(stats.SampleRows.Count > 0);
    }

    [Fact]
    public async Task GetStatisticsAsync_DetectsDelimiter()
    {
        var tempFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Test" } };
        await svc.ExportToCsvAsync(data, tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var stats = await svc.GetStatisticsAsync(tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(stats.DetectedDelimiter);
        Assert.Equal(',', stats.DetectedDelimiter);
    }

    // Feature 6: Chunked/batch processing tests
    [Fact]
    public async Task ProcessFileInChunksAsync_ProcessesInChunks()
    {
        var tempFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data = Enumerable.Range(1, 10).Select(i => new Person { Id = i, Name = $"Person{i}" }).ToList();
        await svc.ExportToCsvAsync(data, tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var chunks = new List<List<Person>>();
        await svc.ProcessFileInChunksAsync<Person>(
            tempFile, 3, async chunk => {
                chunks.Add(chunk.ToList());
                await Task.CompletedTask.ConfigureAwait(false);
            }, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.Equal(4, chunks.Count); // 3 + 3 + 3 + 1
        Assert.Equal(3, chunks[0].Count);
        Assert.Equal(3, chunks[1].Count);
        Assert.Equal(3, chunks[2].Count);
        Assert.Single(chunks[3]);
    }

    [Fact]
    public async Task ProcessStreamInChunksAsync_ProcessesInChunks()
    {
        var svc = new CsvService(_logger);
        var data = Enumerable.Range(1, 7).Select(i => new Person { Id = i, Name = $"Person{i}" }).ToList();
        var bytes = await svc.ExportToCsvBytesAsync(data, TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream(bytes);
        var chunks = new List<List<Person>>();
        await svc.ProcessStreamInChunksAsync<Person>(
            ms, 2, async chunk => {
                chunks.Add(chunk.ToList());
                await Task.CompletedTask.ConfigureAwait(false);
            }, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.Equal(4, chunks.Count); // 2 + 2 + 2 + 1
    }

    // Feature 7: CSV validation tests
    [Fact]
    public async Task ValidateAsync_ValidatesRequiredColumns()
    {
        var tempFile = await _tempSession.CreateFileAsync("Id,Name\n1,Alice\n2,Bob", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var schema = new CsvSchema {
            Columns = [new() { Name = "Id", Required = true }, new() { Name = "Name", Required = true }, new() { Name = "Email", Required = true }], RequireAllColumns = true
        };

        var result = await svc.ValidateAsync(tempFile, schema, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsValid);
        Assert.Contains("Email", result.Errors!.First());
    }

    [Fact]
    public async Task ValidateAsync_ValidatesColumnValues()
    {
        var tempFile = await _tempSession.CreateFileAsync("Id,Name\n1,Alice\ninvalid,Bob", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var schema = new CsvSchema { Columns = [new("Id", true, v => int.TryParse((string?)v, out var _)), new() { Name = "Name", Required = true }] };
        var result = await svc.ValidateAsync(tempFile, schema, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors!, e => e.Contains("Id") || e.Contains("validation"));
    }

    [Fact]
    public async Task ValidateAsync_PassesValidSchema()
    {
        var tempFile = await _tempSession.CreateFileAsync("Id,Name\n1,Alice\n2,Bob", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var schema = new CsvSchema { Columns = [new() { Name = "Id", Required = true }, new() { Name = "Name", Required = true }] };
        var result = await svc.ValidateAsync(tempFile, schema, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors!);
    }

    // Feature 8: Column mapping tests
    [Fact]
    public async Task ParseFileWithMappingAsync_MapsColumnsCorrectly()
    {
        var tempFile = await _tempSession.CreateFileAsync("ID,FullName,Age\n1,Alice Smith,30\n2,Bob Jones,25", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var mappings = new List<ColumnMapping> {
            new() { SourceColumn = "ID", TargetProperty = "Id", Transformer = v => int.Parse(v) },
            new() { SourceColumn = "FullName", TargetProperty = "Name" },
            new() { SourceColumn = "Age", TargetProperty = "Age", Transformer = v => int.Parse(v) }
        };

        var records = await svc.ParseFileWithMappingAsync<Person>(tempFile, mappings, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, records.Count);
        Assert.Equal(1, records[0].Id);
        Assert.Equal("Alice Smith", records[0].Name);
        Assert.Equal(30, records[0].Age);
    }

    [Fact]
    public async Task ParseStreamWithMappingAsync_UsesDefaultValues()
    {
        var csv = "ID,Name\n1,Alice\n2,";
        var svc = new CsvService(_logger);
        using var ms = StringToStream(csv);
        var mappings = new List<ColumnMapping> {
            new() { SourceColumn = "ID", TargetProperty = "Id", Transformer = v => int.Parse(v) },
            new() { SourceColumn = "Name", TargetProperty = "Name", DefaultValue = "Unknown" }
        };

        var records = await svc.ParseStreamWithMappingAsync<Person>(ms, mappings, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, records.Count);
        Assert.Equal("Alice", records[0].Name);
        Assert.Equal("Unknown", records[1].Name);
    }

    // Feature 9: CSV comparison tests
    [Fact]
    public async Task CompareFilesAsync_DetectsIdenticalFiles()
    {
        var tempFile1 = _tempSession.GetFilePath();
        var tempFile2 = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Alice" }, new() { Id = 2, Name = "Bob" } };
        await svc.ExportToCsvAsync(data, tempFile1, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await svc.ExportToCsvAsync(data, tempFile2, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await svc.CompareFilesAsync(tempFile1, tempFile2, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.AreIdentical);
        Assert.Empty(result.Differences);
    }

    [Fact]
    public async Task CompareFilesAsync_DetectsDifferences()
    {
        var tempFile1 = _tempSession.GetFilePath();
        var tempFile2 = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data1 = new List<Person> { new() { Id = 1, Name = "Alice" } };
        var data2 = new List<Person> { new() { Id = 1, Name = "Bob" } };
        await svc.ExportToCsvAsync(data1, tempFile1, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await svc.ExportToCsvAsync(data2, tempFile2, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await svc.CompareFilesAsync(tempFile1, tempFile2, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.AreIdentical);
        Assert.NotEmpty(result.Differences);
    }

    [Fact]
    public async Task CompareFilesAsync_ComparesByKeyColumn()
    {
        var tempFile1 = await _tempSession.CreateFileAsync("Id,Name\n1,Alice\n2,Bob", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var tempFile2 = await _tempSession.CreateFileAsync("Id,Name\n1,Alice\n2,Charlie", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var result = await svc.CompareFilesAsync(tempFile1, tempFile2, "Id", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.AreIdentical);
        Assert.Contains(result.Differences, d => d.Type == DifferenceType.Modified);
    }

    // Feature 10: Append to CSV tests
    [Fact]
    public async Task AppendToCsvAsync_AppendsToExistingFile()
    {
        var tempFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var initialData = new List<Person> { new() { Id = 1, Name = "Alice" } };
        await svc.ExportToCsvAsync(initialData, tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var appendData = new List<Person> { new() { Id = 2, Name = "Bob" } };
        await svc.AppendToCsvAsync(appendData, tempFile, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var allRecords = svc.ParseFile<Person>(tempFile).ToList();
        Assert.Equal(2, allRecords.Count);
        Assert.Equal("Alice", allRecords[0].Name);
        Assert.Equal("Bob", allRecords[1].Name);
    }

    [Fact]
    public async Task AppendToCsvAsync_IncludesHeaderIfMissing()
    {
        var tempFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data = new List<Person> { new() { Id = 1, Name = "Alice" } };
        await svc.AppendToCsvAsync(data, tempFile, true, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var content = await File.ReadAllTextAsync(tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Contains("Id", content);
        Assert.Contains("Name", content);
        Assert.Contains("Alice", content);
    }

    // Feature 12: Multiple file operations tests
    [Fact]
    public async Task CombineCsvFilesAsync_CombinesMultipleFiles()
    {
        var tempFile1 = _tempSession.GetFilePath();
        var tempFile2 = _tempSession.GetFilePath();
        var outputFile = _tempSession.GetFilePath();
        var svc = new CsvService(_logger);
        var data1 = new List<Person> { new() { Id = 1, Name = "Alice" } };
        var data2 = new List<Person> { new() { Id = 2, Name = "Bob" } };
        await svc.ExportToCsvAsync(data1, tempFile1, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await svc.ExportToCsvAsync(data2, tempFile2, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await svc.CombineCsvFilesAsync([tempFile1, tempFile2], outputFile, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var combined = svc.ParseFile<Person>(outputFile).ToList();
        Assert.Equal(2, combined.Count);
        Assert.Equal("Alice", combined[0].Name);
        Assert.Equal("Bob", combined[1].Name);
    }

    [Fact]
    public async Task SplitCsvFileAsync_SplitsIntoMultipleFiles()
    {
        var tempFile = _tempSession.GetFilePath();
        var outputDir = await _tempSession.CreateDirectoryAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var data = Enumerable.Range(1, 10).Select(i => new Person { Id = i, Name = $"Person{i}" }).ToList();
        await svc.ExportToCsvAsync(data, tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await svc.SplitCsvFileAsync(tempFile, 3, outputDir, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var files = Directory.GetFiles(outputDir, "*.csv");
        Assert.True(files.Length >= 3); // Should create multiple files

        // Verify first file has correct data
        var firstFile = files.OrderBy(f => f).First();
        var firstFileData = svc.ParseFile<Person>(firstFile).ToList();
        Assert.Equal(3, firstFileData.Count);
    }

    // Additional edge case tests
    [Fact]
    public async Task ParseStreamStreamingAsync_HandlesEmptyStream()
    {
        var svc = new CsvService(_logger);
        using var ms = new MemoryStream();
        var records = new List<Person>();
        await foreach (var record in svc.ParseStreamStreamingAsync<Person>(ms, ct: TestContext.Current.CancellationToken).ConfigureAwait(false))
            records.Add(record);

        Assert.Empty(records);
    }

    [Fact]
    public async Task GetStatisticsAsync_HandlesEmptyFile()
    {
        var tempFile = await _tempSession.CreateFileAsync("Id,Name\n", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new CsvService(_logger);
        var stats = await svc.GetStatisticsAsync(tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(0, stats.RowCount);
        Assert.Equal(2, stats.ColumnCount);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithStream_ReturnsStatistics()
    {
        var csv = "A,B,C\n1,2,3\n4,5,6\n";
        var svc = new CsvService(_logger);
        using var ms = StringToStream(csv);
        var stats = await svc.GetStatisticsAsync(ms, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, stats.RowCount);
        Assert.Equal(3, stats.ColumnCount);
    }

    [Fact]
    public void ExportToCsvFromDictionary_RoundTrips()
    {
        var svc = new CsvService(_logger);
        var data = new Dictionary<int, IReadOnlyDictionary<int, string>> {
            [0] = new Dictionary<int, string> { [0] = "H1", [1] = "H2" },
            [1] = new Dictionary<int, string> { [0] = "a", [1] = "b" },
            [2] = new Dictionary<int, string> { [0] = "x", [1] = "y" }
        };

        var path = _tempSession.GetFilePath("dict-export.csv");
        svc.ExportToCsvFromDictionary(data, path);
        Assert.True(File.Exists(path));
        var dict = svc.ParseFileAsDictionary(path);
        Assert.Equal(3, dict.Count);
        Assert.Equal("H1", dict[0][0]);
        Assert.Equal("a", dict[1][0]);
        Assert.Equal("x", dict[2][0]);
    }

    [Fact]
    public void ExportToCsvFromDataTable_RoundTrips()
    {
        var svc = new CsvService(_logger);
        var dt = new DataTable.Models.DataTable();
        dt.SetHeader(0, "X").SetHeader(1, "Y");
        dt.AddRow().SetCell(0, "1").SetCell(1, "2");
        dt.AddRow().SetCell(0, "3").SetCell(1, "4");
        var path = _tempSession.GetFilePath("dt-export.csv");
        svc.ExportToCsvFromDataTable(dt, path);
        Assert.True(File.Exists(path));
        var result = svc.ParseFileAsDataTable(path);
        Assert.True(result.IsSuccess);
        var parsed = result.ValueOrThrow();
        Assert.Equal(2, parsed.Headers.Count);
        Assert.Equal(2, parsed.Rows.Count);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("4", parsed.Rows[1][1].DisplayValue);
    }

    [Fact]
    public async Task ValidateAsync_HandlesEmptyFile()
    {
        var tempFile = _tempSession.TouchFile();
        var svc = new CsvService(_logger);
        var schema = new CsvSchema { Columns = [new() { Name = "Id" }] };
        var result = await svc.ValidateAsync(tempFile, schema, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsValid);
        Assert.Contains("empty", result.Errors!.First(), StringComparison.OrdinalIgnoreCase);
    }

    // Feature 2: Progress reporting tests
    // Synchronous progress reporter to avoid flakiness from async callback invocation
    private class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler) => _handler = handler;

        public void Report(T value) => _handler(value);
    }
}