using Lyo.IO.Temp.Models;
using Lyo.Pdf.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.Pdf.Tests;

public class PdfServiceTests : IDisposable, IAsyncDisposable
{
    private readonly PdfService _service;
    private readonly IOTempSession _tempSession;
    private readonly string _testPdfsDir;

    public PdfServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var rootDir = Path.Combine(Path.GetTempPath(), "lyo-pdf-tests");
        Directory.CreateDirectory(rootDir);
        _tempSession = new(new() { RootDirectory = rootDir, FileExtension = ".pdf" }, loggerFactory.CreateLogger<IOTempSession>());
        _service = new(loggerFactory);
        var baseDir = AppContext.BaseDirectory;
        _testPdfsDir = Path.Combine(baseDir, "TestPdfs");
    }

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync();

    public void Dispose() => _tempSession.Dispose();

    private string[] GetTestPdfPaths()
    {
        if (!Directory.Exists(_testPdfsDir))
            return [];

        return Directory.GetFiles(_testPdfsDir, "*.pdf");
    }

    /// <summary>One-page PdfPig-openable PDF whose <see cref="IPdfDocumentText" /> is only needed for synthetic word/layout tests.</summary>
    private IPdfReader OpenBlankReadPdf()
    {
        byte[] bytes;
        using (var editable = _service.CreateEmpty())
            bytes = editable.ToBytes();

        return _service.OpenFromBytes(bytes);
    }

    [Fact]
    public void LoadPdfFromFile_ValidPdf_ReturnsLeaseWithValidId()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        Assert.True(pdf.GetInfo().PageCount >= 1);
    }

    [Fact]
    public async Task LoadPdfFromFileAsync_ValidPdf_ReturnsLeaseWithValidId()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        await using var pdf = await _service.OpenFromFileAsync(paths[0], TestContext.Current.CancellationToken);
        Assert.True(pdf.GetInfo().PageCount >= 1);
    }

    [Fact]
    public void GetPdfInfo_LoadedPdf_ReturnsInfoWithPageCount()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var info = pdf.GetInfo();
        Assert.NotNull(info);
        Assert.True(info.PageCount >= 1);
    }

    [Fact]
    public void GetWords_LoadedPdf_ReturnsWords()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var words = pdf.Text.GetWords();
        Assert.NotNull(words);
    }

    [Fact]
    public void SavePdf_LoadedPdf_WritesToIOTempFile()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var outputPath = _tempSession.GetFilePath("output.pdf");
        File.WriteAllBytes(outputPath, pdf.SourceBytes.ToArray());
        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);
    }

    [Fact]
    public async Task SavePdfAsync_LoadedPdf_WritesToIOTempFile()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        await using var pdf = await _service.OpenFromFileAsync(paths[0], TestContext.Current.CancellationToken);
        var outputPath = _tempSession.GetFilePath("output-async.pdf");
        await File.WriteAllBytesAsync(outputPath, pdf.SourceBytes.ToArray(), TestContext.Current.CancellationToken);
        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);
    }

    [Fact]
    public void LoadPdfsFromFiles_MultiplePdfs_ReturnsAllLeases()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length < 2)
            return;

        var docs = _service.OpenFromFiles(paths.Take(2).ToArray());
        Assert.Equal(2, docs.Count);
        foreach (var pdf in docs)
            pdf.Dispose();
    }

    [Fact]
    public void MergePdfsToFile_TwoLoadedPdfs_WritesMergedToIOTemp()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length < 2)
            return;

        var docs = _service.OpenFromFiles(paths[0], paths[1]);
        try {
            var outputPath = _tempSession.GetFilePath("merged.pdf");
            var buffers = docs.Select(d => d.SourceBytes.ToArray()).ToList();
            var bytes = _service.MergePdfsToFile(buffers, outputPath);
            Assert.True(bytes.Length > 0);
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally {
            foreach (var pdf in docs)
                pdf.Dispose();
        }
    }

    [Fact]
    public void LoadPdfFromBytes_ValidBytes_ReturnsLease()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        var bytes = File.ReadAllBytes(paths[0]);
        using var pdf = _service.OpenFromBytes(bytes);
        Assert.True(pdf.GetInfo().PageCount >= 1);
    }

    [Fact]
    public void LoadPdfFromStream_ValidStream_ReturnsLease()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var stream = File.OpenRead(paths[0]);
        using var pdf = _service.OpenFromStream(stream);
        Assert.True(pdf.GetInfo().PageCount >= 1);
    }

    [Fact]
    public void GetPdfBytes_LoadedPdf_ReturnsByteArray()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var bytes = pdf.SourceBytes.ToArray();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GetLines_LoadedPdf_ReturnsLines()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var lines = pdf.Text.GetLines();
        Assert.NotNull(lines);
        Assert.True(lines.Count > 0, "GetLines should return at least one line");
        Assert.All(
            lines, line => {
                Assert.NotNull(line.Words);
                Assert.False(string.IsNullOrWhiteSpace(line.Text));
            });
    }

    [Fact]
    public void GetLines_WithPage_ReturnsLinesForPage()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var lines = pdf.Text.GetLines(1);
        Assert.NotNull(lines);
        Assert.True(lines.Count > 0, "GetLines with page should return lines for that page");
    }

    [Fact]
    public void GetLinesBetween_LoadedPdf_ReturnsLinesInRange()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var lines = pdf.Text.GetLinesBetween(null, null, 1);
        Assert.NotNull(lines);
        Assert.True(lines.Count > 0, "GetLinesBetween with null boundaries should return all lines on page");
        Assert.All(lines, line => Assert.NotNull(line.Text));
    }

    [Fact]
    public void GetWordsBetween_LoadedPdf_ReturnsWordsInRange()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var words = pdf.Text.GetWordsBetween(null, null, 1);
        Assert.NotNull(words);
        Assert.True(words.Count > 0, "GetWordsBetween with null boundaries should return all words on page");
        Assert.All(words, w => Assert.False(string.IsNullOrWhiteSpace(w.Text)));
    }

    [Fact]
    public void ExtractKeyValuePairs_ByPdfId_ReturnsStructuredResults()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        string[] keys = ["Name:", "Date of Birth:", "County:", "Case Status:"];
        var results = pdf.Text.ExtractKeyValuePairs(keys, 1);
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.All(
            results, r => {
                Assert.NotNull(r.Values);
            });

        var first = results[0];
        Assert.True(first.Values.Count >= 0, "KvColumnResult should have Values dictionary");
    }

    [Fact]
    public void ExtractKeyValuePairs_ByWords_ReturnsStructuredResults()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var words = pdf.Text.GetWords(1);
        string[] keys = ["Name:", "Date of Birth:", "County:"];
        var results = pdf.Text.ExtractKeyValuePairs(words, keys);
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        var first = results[0];
        Assert.NotNull(first.Values);
    }

    [Fact]
    public void ExtractTable_ByPdfId_ReturnsRowsWithExpectedStructure()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        ColumnHeader[] headers = [new("Col1"), new("Col2", true), new("Col3")];
        var rows = pdf.Text.ExtractTable(headers, 1);
        Assert.NotNull(rows);
        foreach (var row in rows) {
            Assert.NotNull(row);
            foreach (var h in headers)
                Assert.True(row.ContainsKey(h.Label), $"Each row should have column '{h.Label}' from headers");
        }
    }

    [Fact]
    public void ExtractTable_ByWords_ReturnsRows()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var words = pdf.Text.GetWords(1);
        ColumnHeader[] headers = [new("A"), new("B")];
        var rows = pdf.Text.ExtractTable(words, headers);
        Assert.NotNull(rows);
        foreach (var row in rows)
            Assert.NotNull(row);
    }

    [Fact]
    public void ExtractKeyValuePairs_VerticalLayout_SameLineValue_ReadsTextAfterKey()
    {
        using var facetPdf = OpenBlankReadPdf();
        static PdfWord W(string text, double left, double right, double top, double bottom) => new(text, new(left, right, top, bottom));

        // Same baseline Y: "Pages:" then value tokens to the right (common in forms).
        var words = new List<PdfWord> { W("Pages:", 10, 52, 100, 96), W("13", 58, 72, 100, 96), W("pages", 74, 108, 100, 96) };
        var results = facetPdf.Text.ExtractKeyValuePairs(words, ["Pages"], 5.0, PdfKeyValueLayout.Vertical);
        Assert.Single(results);
        Assert.True(results[0].Values.TryGetValue("Pages", out var v));
        Assert.NotNull(v);
        Assert.Contains("13", v, StringComparison.Ordinal);
    }

    [Fact]
    public void InferKeyValuePairsFromFormatting_UsesFontNameBoldVsRegular()
    {
        using var facetPdf = OpenBlankReadPdf();
        static PdfWord W(string text, string fontName, double left, double right, double top, double bottom, bool fdBold)
            => new(text, new(left, right, top, bottom), new(11, fontName, fdBold));

        // Stacked label (bold) then value (regular), then same-line bold label + regular value.
        var words = new List<PdfWord> {
            W("Name", "Lato-Bold", 10, 80, 200, 190, true),
            W("John", "Lato-Regular", 10, 60, 185, 175, false),
            W("Role", "Lato-Bold", 10, 50, 170, 160, true),
            W("Dev", "Lato-Regular", 55, 100, 170, 160, false)
        };

        var dict = facetPdf.Text.InferKeyValuePairsFromFormatting(words);
        Assert.True(dict.TryGetValue("Name", out var n) && n != null && n.Contains("John", StringComparison.Ordinal));
        Assert.True(dict.TryGetValue("Role", out var r) && r != null && r.Contains("Dev", StringComparison.Ordinal));
    }

    [Fact]
    public void InferKeyValuePairsFromFormatting_EqualsDelimiter_CustomOrder()
    {
        using var facetPdf = OpenBlankReadPdf();
        static PdfWord W(string text, double left, double right, double top, double bottom) => new(text, new(left, right, top, bottom), new(11, "Lato-Regular"));

        var words = new List<PdfWord> { W("Name = Alice", 10, 100, 200, 190), W("Role = Dev", 10, 100, 170, 160) };
        var dict = facetPdf.Text.InferKeyValuePairsFromFormatting(words, 5.0, 1, PdfInferFormattingFlags.Semicolon, ['=']);
        Assert.True(dict.TryGetValue("Name", out var n) && n != null && n.Contains("Alice", StringComparison.Ordinal));
        Assert.True(dict.TryGetValue("Role", out var r) && r != null && r.Contains("Dev", StringComparison.Ordinal));
    }

    [Fact]
    public void InferKeyValuePairsFromFormatting_ColonTerminated_NoBold()
    {
        using var facetPdf = OpenBlankReadPdf();
        static PdfWord W(string text, double left, double right, double top, double bottom) => new(text, new(left, right, top, bottom), new(11, "Lato-Regular"));

        var words = new List<PdfWord> {
            W("Department:", 10, 100, 200, 190),
            W("Engineering", 10, 100, 185, 175),
            W("Status:", 10, 50, 160, 150),
            W("Active", 55, 100, 160, 150)
        };

        var dict = facetPdf.Text.InferKeyValuePairsFromFormatting(words);
        Assert.True(dict.TryGetValue("Department", out var d) && d != null && d.Contains("Engineering", StringComparison.Ordinal));
        Assert.True(dict.TryGetValue("Status", out var s) && s == "Active");
    }

    [Fact]
    public void InferKeyValuePairsFromFormatting_None_ReturnsEmpty()
    {
        using var facetPdf = OpenBlankReadPdf();
        static PdfWord W(string text, string fontName, double left, double right, double top, double bottom, bool fdBold)
            => new(text, new(left, right, top, bottom), new(11, fontName, fdBold));

        var words = new List<PdfWord> { W("Name", "Lato-Bold", 10, 80, 200, 190, true), W("John", "Lato-Regular", 10, 60, 185, 175, false) };
        var dict = facetPdf.Text.InferKeyValuePairsFromFormatting(words, 5.0, 1, PdfInferFormattingFlags.None);
        Assert.Empty(dict);
    }

    [Fact]
    public void InferKeyValuePairsFromFormatting_BoldOnly_IgnoresDelimiterLines()
    {
        using var facetPdf = OpenBlankReadPdf();
        static PdfWord W(string text, double left, double right, double top, double bottom) => new(text, new(left, right, top, bottom), new(11, "Lato-Regular"));

        var words = new List<PdfWord> { W("Department:", 10, 100, 200, 190), W("Engineering", 10, 100, 185, 175) };
        var dict = facetPdf.Text.InferKeyValuePairsFromFormatting(words, 5.0, 1, PdfInferFormattingFlags.Bold);
        Assert.Empty(dict);
    }

    [Fact]
    public void ExtractKeyValuePairs_VerticalLayout_ReadsValueBelowKey()
    {
        using var facetPdf = OpenBlankReadPdf();
        static PdfWord W(string text, double left, double right, double top, double bottom) => new(text, new(left, right, top, bottom));

        // Higher top = higher on page; key row then value row below (smaller Y centroid).
        var words = new List<PdfWord> { W("Field:", 10, 52, 102, 98), W("Hello", 12, 48, 89, 85) };
        var results = facetPdf.Text.ExtractKeyValuePairs(words, ["Field:"], 5.0, PdfKeyValueLayout.Vertical);
        Assert.Single(results);
        Assert.True(results[0].Values.TryGetValue("Field:", out var v));
        Assert.Equal("Hello", v);
    }

    [Fact]
    public void ExtractKeyValuePairs_EmptyKeys_ReturnsEmptyResult()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var words = pdf.Text.GetWords(1);
        var results = pdf.Text.ExtractKeyValuePairs(words, []);
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Empty(results[0].Values);
    }

    [Fact]
    public void ExtractTable_EmptyWords_ReturnsEmptyList()
    {
        using var facetPdf = OpenBlankReadPdf();
        ColumnHeader[] headers = [new("Col1"), new("Col2")];
        IReadOnlyList<PdfWord> emptyWords = [];
        var rows = facetPdf.Text.ExtractTable(emptyWords, headers);
        Assert.NotNull(rows);
        Assert.Empty(rows);
    }

    [Fact]
    public void ParseBytesAsDataTable_ValidPdf_ReturnsSuccessfulResult()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        var bytes = File.ReadAllBytes(paths[0]);
        ColumnHeader[] headers = [new("A"), new("B")];
        using var facetPdf = OpenBlankReadPdf();
        var result = facetPdf.Text.ParseBytesAsDataTable(bytes, headers, 1);
        Assert.True(result.IsSuccess);
        var dt = result.ValueOrThrow();
        Assert.NotNull(dt.Headers);
        Assert.NotNull(dt.Rows);
    }

    [Fact]
    public void ExtractDataTable_ByPdfId_ReturnsDataTable()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        ColumnHeader[] headers = [new("A"), new("B")];
        var dt = pdf.Text.ExtractDataTable(headers, 1);
        Assert.NotNull(dt);
        Assert.NotNull(dt.Headers);
        Assert.Equal(2, dt.Headers.Count);
        Assert.Equal("A", dt.Headers[0].DisplayValue);
        Assert.Equal("B", dt.Headers[1].DisplayValue);
        Assert.NotNull(dt.Rows);
    }

    [Fact]
    public void ExtractDataTable_ByWords_ReturnsDataTable()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var words = pdf.Text.GetWords(1);
        ColumnHeader[] headers = [new("A"), new("B")];
        var dt = pdf.Text.ExtractDataTable(words, headers);
        Assert.NotNull(dt);
        Assert.Equal(2, dt.Headers.Count);
        Assert.NotNull(dt.Rows);
    }

    [Fact]
    public void ExtractDataTable_EmptyWords_ReturnsEmptyDataTable()
    {
        using var facetPdf = OpenBlankReadPdf();
        ColumnHeader[] headers = [new("Col1"), new("Col2")];
        IReadOnlyList<PdfWord> emptyWords = [];
        var dt = facetPdf.Text.ExtractDataTable(emptyWords, headers);
        Assert.NotNull(dt);
        Assert.Equal(2, dt.Headers.Count);
        Assert.Empty(dt.Rows);
    }

    [Fact]
    public void GetSection_WhenSectionNotFound_ReturnsNull()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        var sections = new[] { "SUMMARY", "NONEXISTENT_XYZ_SECTION", "FOOTER" };
        var section = pdf.Text.GetSection("NONEXISTENT_XYZ_SECTION", sections);
        Assert.Null(section);
    }

    [Fact]
    public void GetSection_WhenSectionExists_ReturnsPdfSectionWithLinesAndWords()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        using var pdf = _service.OpenFromFile(paths[0]);
        _ = pdf.GetInfo();
        var lines = pdf.Text.GetLines(1);
        if (lines.Count == 0)
            return;

        var firstLineText = lines[0].Text.Trim();
        if (string.IsNullOrEmpty(firstLineText) || firstLineText.Length < 2)
            return;

        var sectionName = firstLineText.Length > 20 ? firstLineText[..20] : firstLineText;
        var sections = new[] { sectionName, "END_MARKER" };
        var section = pdf.Text.GetSection(sectionName, sections);
        if (section != null) {
            Assert.Equal(sectionName, section.Name);
            Assert.True(section.StartPage >= 1);
            Assert.True(section.EndPage >= section.StartPage);
            Assert.NotNull(section.Lines);
            Assert.NotNull(section.Words);
        }
    }

    [Fact]
    public async Task GetSectionAsync_WhenSectionNotFound_ReturnsNull()
    {
        var paths = GetTestPdfPaths();
        if (paths.Length == 0)
            return;

        await using var pdf = await _service.OpenFromFileAsync(paths[0], TestContext.Current.CancellationToken);
        var sections = new[] { "NO_SUCH_SECTION_123" };
        var section = await pdf.Text.GetSectionAsync("NO_SUCH_SECTION_123", sections, ct: TestContext.Current.CancellationToken);
        Assert.Null(section);
    }

    [Fact]
    public void InferTableHeadersFromFormatting_TwoLineUnderlinedHeader_MergesStackedLinesIntoColumnLabels()
    {
        using var facetPdf = OpenBlankReadPdf();
        var ul = new PdfWordFormat(FontUnderline: true);

        static PdfWord W(string text, double left, double right, double top, double bottom, PdfWordFormat? f = null) => new(text, new(left, right, top, bottom), f);

        var words = new List<PdfWord> {
            W("Case", 10, 35, 102, 98, ul),
            W("Calendar", 100, 155, 102, 98, ul),
            W("Schedule", 250, 325, 102, 98, ul),
            W("Event", 12, 58, 90, 86, ul),
            W("Type", 105, 148, 90, 86, ul),
            W("Start", 258, 305, 90, 86, ul)
        };

        var headers = facetPdf.Text.InferTableHeadersFromFormatting(words, 5.0, PdfInferFormattingFlags.Underline);
        Assert.Equal(3, headers.Length);
        Assert.Equal("Case Event", headers[0].Label);
        Assert.Equal("Calendar Type", headers[1].Label);
        Assert.Equal("Schedule Start", headers[2].Label);
    }

    [Fact]
    public void InferTableHeadersFromFormatting_DoesNotMergeBoldHeaderWithUnstyledDataRowBelow()
    {
        using var facetPdf = OpenBlankReadPdf();
        var bold = new PdfWordFormat(FontBold: true);

        static PdfWord W(string text, double left, double right, double top, double bottom, PdfWordFormat? f = null) => new(text, new(left, right, top, bottom), f);

        var words = new List<PdfWord> {
            W("TIMESTAMP", 20, 120, 100, 96, bold),
            W("AUDIT", 200, 260, 100, 96, bold),
            W("04/13/2026", 20, 100, 88, 84),
            W("someone", 200, 280, 88, 84)
        };

        var headers = facetPdf.Text.InferTableHeadersFromFormatting(words, 5.0, PdfInferFormattingFlags.Bold);
        Assert.Equal(2, headers.Length);
        Assert.Equal("TIMESTAMP", headers[0].Label);
        Assert.Equal("AUDIT", headers[1].Label);
    }

    [Fact]
    public void InferTableHeadersFromFormatting_SecondRowMustMatchInferenceStyleToMerge()
    {
        using var facetPdf = OpenBlankReadPdf();
        var ul = new PdfWordFormat(FontUnderline: true);

        static PdfWord W(string text, double left, double right, double top, double bottom, PdfWordFormat? f = null) => new(text, new(left, right, top, bottom), f);

        // Use the same X banding as TwoLineUnderlinedHeader so horizontal gap clustering yields three columns (tight
        // first-gap layouts merge into two columns because min gutter is adaptive to median gap).
        var wordsPlainSecondRow = new List<PdfWord> {
            W("Case", 10, 35, 102, 98, ul),
            W("Calendar", 100, 155, 102, 98, ul),
            W("Schedule", 250, 325, 102, 98, ul),
            W("Event", 12, 58, 90, 86),
            W("Type", 105, 148, 90, 86),
            W("Start", 258, 305, 90, 86)
        };

        var h1 = facetPdf.Text.InferTableHeadersFromFormatting(wordsPlainSecondRow, 5.0, PdfInferFormattingFlags.Underline);
        Assert.Equal(3, h1.Length);
        Assert.Equal("Case", h1[0].Label);
        Assert.Equal("Calendar", h1[1].Label);
        Assert.Equal("Schedule", h1[2].Label);
        var wordsBothRowsUnderlined = new List<PdfWord> {
            W("Case", 10, 35, 102, 98, ul),
            W("Calendar", 100, 155, 102, 98, ul),
            W("Schedule", 250, 325, 102, 98, ul),
            W("Event", 12, 58, 90, 86, ul),
            W("Type", 105, 148, 90, 86, ul),
            W("Start", 258, 305, 90, 86, ul)
        };

        var h2 = facetPdf.Text.InferTableHeadersFromFormatting(wordsBothRowsUnderlined, 5.0, PdfInferFormattingFlags.Underline);
        Assert.True(h2.Length >= 3);
        var flat = string.Join(' ', h2.Select(x => x.Label));
        Assert.Contains("Event", flat, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Case", flat, StringComparison.OrdinalIgnoreCase);
    }
}