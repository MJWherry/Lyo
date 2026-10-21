using Lyo.Formatter.Web.Components;

namespace Lyo.Formatter.Tests;

public class LyoFormatterContextCatalogTests
{
    private static readonly LyoFormatterContextEntry[] Catalog = [
        new("cli", "x", false), new("Count", "1", false), new("Date", "2026-08-16", false), new("Download", "file", false), new("Name", "Ada", false),
        new("User.Address.City", "London", false)
    ];

    /// <summary>
    /// The default budget suits a session context of a handful of keys. A workflow step's context is far wider — every dependency contributes a whole run shape — and truncating
    /// it drops the paths the user came for, so a caller that knows its context is large can ask for more.
    /// </summary>
    [Fact]
    public void Build_RaisedBudget_KeepsPathsTheDefaultBudgetWouldTruncate()
    {
        var context = Enumerable.Range(0, 200).ToDictionary(i => $"Key{i:D3}", i => (object?)i);
        var capped = LyoFormatterContextCatalog.Build(context).Select(e => e.Path).ToList();
        var raised = LyoFormatterContextCatalog.Build(context, 250).Select(e => e.Path).ToList();
        Assert.DoesNotContain("Key199", capped);
        Assert.Contains("Key199", raised);
    }

    [Fact]
    public void Build_BudgetSmallerThanTheClockRows_StillStopsAtTheBudget()
    {
        var rows = LyoFormatterContextCatalog.Build(new Dictionary<string, object?> { ["Name"] = "Ada" }, 1);
        Assert.Single(rows);
    }

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
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Do}", 3, out var span));
        Assert.True(span.Closed);
        Assert.Equal("Do", span.Prefix);
    }

    [Fact]
    public void Suggest_TypedPrefixInsideClosedToken_DropsNonMatchingKeys()
    {
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Do}", 3, out var span));
        var rows = LyoFormatterContextCatalog.Suggest(Catalog, span);
        Assert.Equal(["Download"], rows.Select(e => e.Path));
    }

    [Fact]
    public void Suggest_ClosedTokenEmptyPrefix_KeepsAllKeys()
    {
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Name}", 1, out var span));
        Assert.True(span.Closed);
        Assert.Equal(string.Empty, span.Prefix);
        var rows = LyoFormatterContextCatalog.Suggest(Catalog, span, 32);
        Assert.Equal(Catalog.Length, rows.Count);
    }

    [Fact]
    public void CaretAfterInsert_NestedValue_LeavesCaretBeforeClose()
    {
        Assert.Equal(5, LyoFormatterContextCatalog.CaretAfterInsert(0, "User", true));
        Assert.Equal("{User}".Length - 1, LyoFormatterContextCatalog.CaretAfterInsert(0, "User", true));
    }

    [Fact]
    public void CaretAfterInsert_LeafValue_LeavesCaretAfterClose() => Assert.Equal("{Name}".Length, LyoFormatterContextCatalog.CaretAfterInsert(0, "Name", false));

    [Fact]
    public void Suggest_OpenTokenPrefixDo_DropsCli()
    {
        Assert.True(LyoFormatterContextCatalog.TryGetPlaceholderAtCaret("{Do", 3, out var span));
        Assert.False(span.Closed);
        var rows = LyoFormatterContextCatalog.Suggest(Catalog, span);
        Assert.DoesNotContain(rows, e => e.Path.Equals("cli", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["Download"], rows.Select(e => e.Path));
    }

    [Fact]
    public void Build_IncludesDateTimeExpressionSuggestions()
    {
        var rows = LyoFormatterContextCatalog.Build(new { Name = "Ada" });
        Assert.Contains(rows, e => e.Path == "DateTime.Now");
        Assert.Contains(rows, e => e.Path == "DateTime.UtcNow");
        Assert.Contains(rows, e => e.Path == "DateTime.Now.AddDays(-1)");
        Assert.Contains(rows, e => e.Path == "Name");
    }
}