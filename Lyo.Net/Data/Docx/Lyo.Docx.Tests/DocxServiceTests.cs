using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Lyo.Formatter;

namespace Lyo.Docx.Tests;

public class DocxServiceTests
{
    private readonly DocxService _docx = new(new FormatterService());

    [Fact]
    public void Read_ParagraphAndTable_ReturnsText()
    {
        var bytes = Document(body => {
            body.AppendChild(Paragraph("Hello"));
            body.AppendChild(new Table(new TableRow(new TableCell(Paragraph("World")))));
        });

        Assert.Equal("Hello\nWorld", _docx.ReadText(bytes));
    }

    [Fact]
    public void Fill_TokenSplitAcrossRuns_ReplacesOnce()
    {
        var bytes = Document(body => body.AppendChild(new Paragraph(
            new Run(new Text("{Na")),
            new Run(new Text("me}")))));

        var filled = _docx.Fill(bytes, new { Name = "Ada" });

        Assert.Equal("Ada", _docx.ReadText(filled));
    }

    [Fact]
    public void Fill_UnknownToken_LeavesRunUnchanged()
    {
        var bytes = Document(body => body.AppendChild(Paragraph("{Missing}")));

        var filled = _docx.Fill(bytes, new { Name = "Ada" });

        Assert.Equal("{Missing}", _docx.ReadText(filled));
    }

    private static Paragraph Paragraph(string text) => new(new Run(new Text(text)));

    private static byte[] Document(Action<Body> build)
    {
        using var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true)) {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(new Body());
            build(main.Document.Body!);
            main.Document.Save();
        }

        return stream.ToArray();
    }
}
