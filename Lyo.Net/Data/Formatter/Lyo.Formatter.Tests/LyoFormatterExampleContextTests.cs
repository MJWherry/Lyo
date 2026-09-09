using Lyo.Formatter.Web.Components;

namespace Lyo.Formatter.Tests;

/// <summary>
/// The host registers the keys only it knows (what a worker binds at run time) and each view adds its own live keys. Merge order decides which wins, and a view that
/// supplies a non-dictionary context cannot be merged at all — both are checked here because getting either wrong silently changes what a template resolves against.
/// </summary>
public class LyoFormatterExampleContextTests
{
    [Fact]
    public void Merge_ViewKeysWinOverHostKeys()
    {
        var host = new LyoFormatterExampleContext().Add("client", "host").Add("region", "eu");
        Dictionary<string, object?> view = new(StringComparer.OrdinalIgnoreCase) { ["client"] = "view" };

        var merged = Assert.IsType<Dictionary<string, object?>>(LyoFormatterExampleContext.Merge(host, view));

        Assert.Equal("view", merged["client"]);
        Assert.Equal("eu", merged["region"]);
    }

    [Fact]
    public void Merge_IsCaseInsensitiveAcrossSides()
    {
        var host = new LyoFormatterExampleContext().Add("Client", "host");
        Dictionary<string, object?> view = new(StringComparer.OrdinalIgnoreCase) { ["client"] = "view" };

        var merged = Assert.IsType<Dictionary<string, object?>>(LyoFormatterExampleContext.Merge(host, view));

        Assert.Equal("view", Assert.Single(merged).Value);
    }

    [Fact]
    public void Merge_NoView_ReturnsHostValues()
    {
        var host = new LyoFormatterExampleContext().Add("client", "host");

        var merged = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(LyoFormatterExampleContext.Merge(host, null));

        Assert.Equal("host", merged["client"]);
    }

    [Fact]
    public void Merge_NoHost_ReturnsViewUnchanged()
    {
        var view = new Dictionary<string, object?> { ["client"] = "view" };

        Assert.Same(view, LyoFormatterExampleContext.Merge(null, view));
        Assert.Same(view, LyoFormatterExampleContext.Merge(new(), view));
    }

    /// <summary>A DTO's keys are its properties. Grafting host keys onto one would need a wrapper that changes every path shown, so the view context replaces it outright.</summary>
    [Fact]
    public void Merge_NonDictionaryView_ReplacesTheHostContext()
    {
        var host = new LyoFormatterExampleContext().Add("client", "host");
        var view = new { Name = "Ada" };

        Assert.Same(view, LyoFormatterExampleContext.Merge(host, view));
    }

    [Fact]
    public void Merge_NeitherSide_ReturnsNull() => Assert.Null(LyoFormatterExampleContext.Merge(null, null));

    [Fact]
    public void Add_SameKeyTwice_KeepsTheLastValue()
    {
        var context = new LyoFormatterExampleContext().Add("client", "first").Add("CLIENT", "second");

        Assert.Equal("second", Assert.Single(context.Values).Value);
    }
}
