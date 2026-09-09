using Lyo.Images.Ocr.Models;

namespace Lyo.Images.Ocr.Tests;

public sealed class OcrLineGrouperTests
{
    [Fact]
    public void GroupIntoLines_Empty_Returns_Empty()
    {
        var lines = OcrLineGrouper.GroupIntoLines(Array.Empty<OcrWord>(), 10);
        Assert.Empty(lines);
    }

    [Fact]
    public void GroupIntoLines_Single_Word_One_Line()
    {
        var w = new OcrWord("hi", new(0, 10, 50, 40), 99);
        var lines = OcrLineGrouper.GroupIntoLines([w], 10);
        Assert.Single(lines);
        Assert.Equal("hi", lines[0].Text);
        Assert.Same(w, lines[0].Words[0]);
    }

    [Fact]
    public void GroupIntoLines_Two_Words_Same_Line_Merge()
    {
        var a = new OcrWord("a", new(0, 5, 50, 45), null);
        var b = new OcrWord("b", new(10, 20, 52, 48), null);
        var lines = OcrLineGrouper.GroupIntoLines([b, a], 15);
        Assert.Single(lines);
        Assert.Equal("a b", lines[0].Text);
    }

    [Fact]
    public void GroupIntoLines_Vertical_Separation_Exceeding_Tolerance_Splits_Lines()
    {
        // Upper line midY = (90 + 70) / 2 = 80. Lower line midY = (30 + 10) / 2 = 20. Delta 60 is above tolerance 10.
        var upper = new OcrWord("TOP", new(0, 100, 90, 70), null);
        var lower = new OcrWord("BOT", new(0, 100, 30, 10), null);
        var lines = OcrLineGrouper.GroupIntoLines([upper, lower], 10);
        Assert.Equal(2, lines.Count);
        Assert.Equal("TOP", lines[0].Text);
        Assert.Equal("BOT", lines[1].Text);
    }

    [Fact]
    public void GroupIntoLines_Orders_Left_To_Right_On_Each_Line()
    {
        var right = new OcrWord("second", new(80, 200, 50, 40), null);
        var left = new OcrWord("first", new(0, 70, 50, 40), null);
        var lines = OcrLineGrouper.GroupIntoLines([right, left], 20);
        Assert.Single(lines);
        Assert.Equal("first second", lines[0].Text);
        Assert.Equal("first", lines[0].Words[0].Text);
        Assert.Equal("second", lines[0].Words[1].Text);
    }

    [Fact]
    public void GroupIntoLines_Omits_Whitespace_Only_Words_From_Line_Text()
    {
        var keep = new OcrWord("ok", new(0, 40, 50, 45), null);
        var spaces = new OcrWord("   ", new(50, 90, 50, 45), null);
        var lines = OcrLineGrouper.GroupIntoLines([keep, spaces], 15);
        Assert.Single(lines);
        Assert.Equal("ok", lines[0].Text);
        Assert.Equal(2, lines[0].Words.Count);
    }

    [Fact]
    public void GroupIntoLines_Null_Word_List_Throws() => Assert.Throws<ArgumentNullException>(() => OcrLineGrouper.GroupIntoLines(null!, 10));
}