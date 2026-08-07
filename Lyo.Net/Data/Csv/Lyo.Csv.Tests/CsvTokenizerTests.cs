using System.Text;
using Lyo.Csv.Models;

namespace Lyo.Csv.Tests;

public class CsvTokenizerTests
{
    [Fact]
    public void Parse_MultilineQuotedField_RoundTrips()
    {
        var csv = "Id,Name\n1,\"line1\nline2\"\n";
        var svc = new CsvService();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Single(rows);
        Assert.Equal("line1\nline2", rows[0].Name);
    }

    [Fact]
    public void Parse_EscapedQuotes_InField()
    {
        var csv = "Id,Name\n1,\"say \"\"hi\"\"\"\n";
        var svc = new CsvService();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Single(rows);
        Assert.Equal("say \"hi\"", rows[0].Name);
    }

    [Fact]
    public void Parse_CrLf_InsideAndBetweenRows()
    {
        var csv = "Id,Name\r\n1,\"a\r\nb\"\r\n";
        var svc = new CsvService();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Single(rows);
        Assert.Equal("a\r\nb", rows[0].Name);
    }

    [Fact]
    public void Parse_SemicolonDelimiter_RoundTrip()
    {
        var svc = new CsvService(options: new CsvOptions { Delimiter = ";" });
        PersonRow[] data = [new() { Id = 1, Name = "A;B" }];
        var bytes = svc.ExportToCsvBytes(data);
        using var ms = new MemoryStream(bytes);
        var parsed = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Single(parsed);
        Assert.Equal("A;B", parsed[0].Name);
        Assert.Contains(';', Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Parse_Utf8Bom_OnRead()
    {
        var payload = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("Id,Name\n1,Bom\n")).ToArray();
        var svc = new CsvService();
        using var ms = new MemoryStream(payload);
        var rows = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Single(rows);
        Assert.Equal("Bom", rows[0].Name);
    }

    [Fact]
    public void Parse_AllowComments_SkipsHashLines()
    {
        var csv = "# comment\nId,Name\n1,Ok\n# trailing\n2,Two\n";
        var svc = new CsvService(options: new CsvOptions { AllowComments = true });
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal("Ok", rows[0].Name);
        Assert.Equal("Two", rows[1].Name);
    }

    [Fact]
    public void Parse_CommentChar_MidField_NotSkipped()
    {
        var csv = "Id,Name\n1,#notacomment\n";
        var svc = new CsvService(options: new CsvOptions { AllowComments = true });
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Single(rows);
        Assert.Equal("#notacomment", rows[0].Name);
    }

    [Fact]
    public void Parse_CustomEscapePrefix_RoundTrip()
    {
        var opts = new CsvOptions { Quote = '"', Escape = '\\' };
        var svc = new CsvService(options: opts);
        PersonRow[] data = [new() { Id = 1, Name = "a\"b" }];
        var bytes = svc.ExportToCsvBytes(data);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\\\"", text);
        using var ms = new MemoryStream(bytes);
        var parsed = svc.ParseStream<PersonRow>(ms).ToList();
        Assert.Single(parsed);
        Assert.Equal("a\"b", parsed[0].Name);
    }

    [Fact]
    public void Parse_TrailingDelimiter_KeepsEmptyField()
    {
        var csv = "A,B\n1,\n";
        var svc = new CsvService();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dict = svc.ParseStreamAsDictionary(ms);
        Assert.Equal(2, dict.Count);
        Assert.Equal("1", dict[1][0]);
        Assert.Equal("", dict[1][1]);
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmpty()
    {
        var svc = new CsvService();
        using var ms = new MemoryStream();
        var dict = svc.ParseStreamAsDictionary(ms);
        Assert.Empty(dict);
    }

    [Fact]
    public async Task ParseRowsStreamingAsync_YieldsCells()
    {
        var csv = "A,B\n1,2\n3,4\n";
        var svc = new CsvService();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = new List<IReadOnlyList<string>>();
        await foreach (var row in svc.ParseStreamRowsStreamingAsync(ms, TestContext.Current.CancellationToken))
            rows.Add(row);

        Assert.Equal(3, rows.Count);
        Assert.Equal(["A", "B"], rows[0]);
        Assert.Equal(["1", "2"], rows[1]);
    }

    [Fact]
    public async Task ExportToCsvAsync_FromAsyncEnumerable_RoundTrip()
    {
        var svc = new CsvService();
        async IAsyncEnumerable<PersonRow> Source()
        {
            yield return new() { Id = 1, Name = "A" };
            await Task.Yield();
            yield return new() { Id = 2, Name = "B" };
        }

        await using var ms = new MemoryStream();
        await svc.ExportToCsvStreamAsync(Source(), ms, TestContext.Current.CancellationToken);
        ms.Position = 0;
        var parsed = await svc.ParseStreamAsync<PersonRow>(ms, TestContext.Current.CancellationToken);
        Assert.Equal(2, parsed.Count);
        Assert.Equal("B", parsed[1].Name);
    }

    private sealed class PersonRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
