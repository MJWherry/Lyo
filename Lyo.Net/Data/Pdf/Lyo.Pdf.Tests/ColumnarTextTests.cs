using Lyo.Pdf.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Pdf.Tests;

public class ColumnarTextTests
{
    private static PdfWord W(string text, double left, double right, double top, double bottom) => new(text, new(left, right, top, bottom));

    private static void UsingBlankPdfText(Action<IPdfDocumentText> body)
    {
        var service = new PdfService(NullLoggerFactory.Instance);
        byte[] bytes;
        using (var editable = service.CreateEmpty())
            bytes = editable.ToBytes();

        using var pdf = service.OpenFromBytes(bytes);
        body(pdf.Text);
    }

    [Fact]
    public void GetColumnarText_TwoColumns_UsesGutterBetweenWords()
    {
        UsingBlankPdfText(text => {
            var words = new List<PdfWord> {
                W("Hello", 10, 50, 800, 790),
                W("World", 400, 460, 800, 790),
                W("Foo", 12, 45, 750, 740),
                W("Bar", 405, 450, 750, 740)
            };

            var result = text.GetColumnarText(words, 2, 5.0);
            Assert.Equal(2, result.Columns.Count);
            Assert.Equal("Hello\nFoo", result.Columns[0]);
            Assert.Equal("World\nBar", result.Columns[1]);
        });
    }

    [Fact]
    public void GetColumnarText_ThreeColumns_EqualWidthBands()
    {
        UsingBlankPdfText(text => {
            var words = new List<PdfWord> { W("A", 0, 20, 100, 90), W("B", 110, 130, 100, 90), W("C", 220, 240, 100, 90) };
            var result = text.GetColumnarText(words, 3, 5.0);
            Assert.Equal(3, result.Columns.Count);
            Assert.Equal("A", result.Columns[0]);
            Assert.Equal("B", result.Columns[1]);
            Assert.Equal("C", result.Columns[2]);
        });
    }

    [Fact]
    public void GetColumnarText_SingleColumn_JoinsLinesWithNewlines()
    {
        UsingBlankPdfText(text => {
            var words = new List<PdfWord> { W("top", 10, 40, 800, 790), W("bottom", 12, 60, 700, 690) };
            var result = text.GetColumnarText(words, 1);
            Assert.Single(result.Columns);
            Assert.Equal("top\nbottom", result.Columns[0]);
        });
    }

    [Fact]
    public void GetColumnarText_EmptyWords_ReturnsEmptyStringsPerColumn()
    {
        UsingBlankPdfText(text => {
            var result = text.GetColumnarText([], 2);
            Assert.Equal(2, result.Columns.Count);
            Assert.Equal("", result.Columns[0]);
            Assert.Equal("", result.Columns[1]);
        });
    }

    [Fact]
    public void GetColumnarText_ColumnCountZero_Throws()
    {
        UsingBlankPdfText(text => { Assert.ThrowsAny<ArgumentOutOfRangeException>(() => text.GetColumnarText([W("x", 0, 10, 100, 90)], 0)); });
    }

    [Fact]
    public void PdfColumnarText_ToCombinedString_JoinsColumns()
    {
        var t = new PdfColumnarText(["a", "b"]);
        Assert.Equal("a\n\nb", t.ToCombinedString());
    }
}
