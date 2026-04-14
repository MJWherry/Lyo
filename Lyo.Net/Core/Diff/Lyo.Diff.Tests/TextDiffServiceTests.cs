using Lyo.Diff.Text;

namespace Lyo.Diff.Tests;

public sealed class TextDiffServiceTests
{
    private readonly ITextDiffService _diff = new TextDiffService(new TextTokenizer());

    [Fact]
    public void LineMode_identical_returns_all_equal_chunks()
    {
        var a = "a\nb\nc";
        var b = "a\nb\nc";
        var r = _diff.Diff(a, b, new() { Mode = TextTokenizeMode.Line });
        Assert.Equal(3, r.Chunks.Length);
        Assert.All(r.Chunks, c => Assert.Equal(TextDiffKind.Equal, c.Kind));
    }

    [Fact]
    public void LineMode_one_line_changed_produces_delete_and_insert()
    {
        var a = "a\nb\nc";
        var b = "a\nx\nc";
        var r = _diff.Diff(a, b, new() { Mode = TextTokenizeMode.Line });
        Assert.Contains(r.Chunks, c => c.Kind == TextDiffKind.Delete);
        Assert.Contains(r.Chunks, c => c.Kind == TextDiffKind.Insert);
    }

    [Fact]
    public void WordMode_whitespace_split_diffs_words()
    {
        var r = _diff.Diff("hello world", "hello there", new() { Mode = TextTokenizeMode.Word });
        Assert.NotEmpty(r.Chunks);
    }

    [Fact]
    public void CharacterMode_single_char_change()
    {
        var r = _diff.Diff("abc", "abx", new() { Mode = TextTokenizeMode.Character });
        Assert.NotEmpty(r.Chunks);
    }

    [Fact]
    public void DefaultMode_single_char_change_uses_character_diff()
    {
        var r = _diff.Diff("abc", "abx");
        Assert.Contains(r.Chunks, c => c.Kind == TextDiffKind.Delete && c.OldStart == 2 && c.OldLength == 1);
        Assert.Contains(r.Chunks, c => c.Kind == TextDiffKind.Insert && c.NewStart == 2 && c.NewLength == 1);
    }

    [Fact]
    public void Max_tokens_throws_when_exceeded()
    {
        var opts = new TextDiffOptions { Mode = TextTokenizeMode.Character, MaxTokensPerSide = 2 };
        Assert.Throws<InvalidOperationException>(() => _diff.Diff("abc", "abc", opts));
    }
}