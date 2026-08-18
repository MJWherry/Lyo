using Lyo.Formatter.Web.Components;

namespace Lyo.Formatter.Tests;

public class LyoFormatterContextCatalogTests
{
    private static readonly LyoFormatterContextEntry[] Catalog = [
        new("cli", "x", false),
        new("Count", "1", false),
        new("Date", "2026-08-16", false),
        new("Download", "file", false),
        new("Name", "Ada", false),
        new("User.Address.City", "London", false)
    ];

    [Fact]
    public void Filter_PrefixDo_DropsKeysThatDoNotStartWithDo()
    {
        var rows = LyoFormatterContextCatalog.Filter(Catalog, "Do");
        Assert.Equal(["Download"], rows.Select(e => e.Path));
    }

    [Fact]
    public void Filter_SegmentPrefix_MatchesNestedPath()
    {
        var rows = LyoFormatterContextCatalog.Filter(Catalog, "City");
        Assert.Equal(["User.Address.City"], rows.Select(e => e.Path));
    }

    [Fact]
    public void TryGetPlaceholderAtCaret_ClosedTokenAfterTyping_PrefixIsTypedText()
    {
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Do}", caret: 3, out var span));
        Assert.True(span.Closed);
        Assert.Equal("Do", span.Prefix);
    }

    [Fact]
    public void Suggest_TypedPrefixInsideClosedToken_DropsNonMatchingKeys()
    {
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Do}", caret: 3, out var span));
        var rows = LyoFormatterContextCatalog.Suggest(Catalog, span);
        Assert.Equal(["Download"], rows.Select(e => e.Path));
    }

    [Fact]
    public void Suggest_ClosedTokenEmptyPrefix_KeepsAllKeys()
    {
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Name}", caret: 1, out var span));
        Assert.True(span.Closed);
        Assert.Equal(string.Empty, span.Prefix);
        var rows = LyoFormatterContextCatalog.Suggest(Catalog, span, limit: 32);
        Assert.Equal(Catalog.Length, rows.Count);
    }

    [Fact]
    public void CaretAfterInsert_NestedValue_LeavesCaretBeforeClose()
    {
        Assert.Equal(5, LyoFormatterContextCatalog.CaretAfterInsert(0, "User", hasChildren: true));
        Assert.Equal("{User}".Length - 1, LyoFormatterContextCatalog.CaretAfterInsert(0, "User", hasChildren: true));
    }

    [Fact]
    public void CaretAfterInsert_LeafValue_LeavesCaretAfterClose()
        => Assert.Equal("{Name}".Length, LyoFormatterContextCatalog.CaretAfterInsert(0, "Name", hasChildren: false));

    [Fact]
    public void Suggest_OpenTokenPrefixDo_DropsCli()
    {
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Do", caret: 3, out var span));
        Assert.False(span.Closed);
        var rows = LyoFormatterContextCatalog.Suggest(Catalog, span);
        Assert.DoesNotContain(rows, e => e.Path.Equals("cli", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["Download"], rows.Select(e => e.Path));
    }
}
