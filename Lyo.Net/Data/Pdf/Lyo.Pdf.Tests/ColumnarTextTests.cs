using Lyo.Pdf.Models;

namespace Lyo.Pdf.Tests;

public class ColumnarTextTests
{
    private static PdfWord W(string text, double left, double right, double top, double bottom) => new(text, new(left, right, top, bottom));

    [Fact]
    public void GetColumnarText_TwoColumns_UsesGutterBetweenWords()
    {
        var service = new PdfService();
        var words = new List<PdfWord> {
            W("Hello", 10, 50, 800, 790),
            W("World", 400, 460, 800, 790),
            W("Foo", 12, 45, 750, 740),
            W("Bar", 405, 450, 750, 740)
        };

        var result = service.GetColumnarText(words, 2, 5.0);
        Assert.Equal(2, result.Columns.Count);
        Assert.Equal("Hello\nFoo", result.Columns[0]);
        Assert.Equal("World\nBar", result.Columns[1]);
    }

    [Fact]
    public void GetColumnarText_ThreeColumns_EqualWidthBands()
    {
        var service = new PdfService();
        var words = new List<PdfWord> { W("A", 0, 20, 100, 90), W("B", 110, 130, 100, 90), W("C", 220, 240, 100, 90) };
        var result = service.GetColumnarText(words, 3, 5.0);
        Assert.Equal(3, result.Columns.Count);
        Assert.Equal("A", result.Columns[0]);
        Assert.Equal("B", result.Columns[1]);
        Assert.Equal("C", result.Columns[2]);
    }

    [Fact]
    public void GetColumnarText_SingleColumn_JoinsLinesWithNewlines()
    {
        var service = new PdfService();
        var words = new List<PdfWord> { W("top", 10, 40, 800, 790), W("bottom", 12, 60, 700, 690) };
        var result = service.GetColumnarText(words, 1);
        Assert.Single(result.Columns);
        Assert.Equal("top\nbottom", result.Columns[0]);
    }

    [Fact]
    public void GetColumnarText_EmptyWords_ReturnsEmptyStringsPerColumn()
    {
        var service = new PdfService();
        var result = service.GetColumnarText([], 2);
        Assert.Equal(2, result.Columns.Count);
        Assert.Equal("", result.Columns[0]);
        Assert.Equal("", result.Columns[1]);
    }

    [Fact]
    public void GetColumnarText_ColumnCountZero_Throws()
    {
        var service = new PdfService();
        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetColumnarText([W("x", 0, 10, 100, 90)], 0));
    }

    [Fact]
    public void PdfColumnarText_ToCombinedString_JoinsColumns()
    {
        var t = new PdfColumnarText(["a", "b"]);
        Assert.Equal("a\n\nb", t.ToCombinedString());
    }
}