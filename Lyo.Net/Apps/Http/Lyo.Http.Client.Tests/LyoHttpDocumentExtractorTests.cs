using Lyo.Http.Client;
using Lyo.Http.Client.Extract;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpDocumentExtractorTests
{
    private static string Html() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "gallery.html"));

    [Fact]
    public void ExtractImages_ResolvesRelative_AndSrcsetFirst()
    {
        var items = LyoHttpDocumentExtractor.ExtractImages(Html(), new() { LinkResolutionBaseUri = "https://cdn.example.test/gallery/" });
        var urls = LyoHttpDocumentExtractor.ToUrlList(items);
        Assert.Contains(urls, u => u.Contains("relative.jpg", StringComparison.Ordinal));
        Assert.Contains(urls, u => u.Contains("a.jpg", StringComparison.Ordinal));
        Assert.DoesNotContain(urls, u => u.Contains("b.jpg", StringComparison.Ordinal));
        Assert.Contains(urls, u => u.Contains("lazy.webp", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtractImages_SrcsetAllCandidates()
    {
        var items = LyoHttpDocumentExtractor.ExtractImages(Html(), new() { SrcsetMode = LyoHttpSrcsetMode.AllCandidates, LinkResolutionBaseUri = "https://cdn.example.test/" });
        var urls = LyoHttpDocumentExtractor.ToUrlList(items);
        Assert.Contains(urls, u => u.Contains("b.jpg", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtractLinks_ExtensionFilter()
    {
        var items = LyoHttpDocumentExtractor.ExtractLinks(Html(), new() {
            FileExtensionFilter = [".zip", ".pdf"],
            LinkResolutionBaseUri = "https://cdn.example.test/"
        });
        var urls = LyoHttpDocumentExtractor.ToUrlList(items);
        Assert.Contains(urls, u => u.Contains("a.zip", StringComparison.Ordinal));
        Assert.Contains(urls, u => u.Contains("notes.pdf", StringComparison.Ordinal));
        Assert.DoesNotContain(urls, u => u.EndsWith("/page", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtractMeta_OgAndCanonical()
    {
        var items = LyoHttpDocumentExtractor.ExtractMeta(Html(), new() { LinkResolutionBaseUri = "https://cdn.example.test/" });
        Assert.Contains(items, i => i.Text == "og:image" && (i.Url ?? i.Raw)!.Contains("og.png"));
        Assert.Contains(items, i => i.Text == "canonical");
    }

    [Fact]
    public void ExtractJsonLd_FindsImageObjectUrl()
    {
        var items = LyoHttpDocumentExtractor.ExtractJsonLd(Html());
        Assert.Contains(items, i => i.Url != null && i.Url.Contains("ld.jpg"));
    }

    [Fact]
    public void ExtractRegex_ScriptUrl()
    {
        var items = LyoHttpDocumentExtractor.ExtractRegex(Html(), new() { Pattern = @"https://cdn\.example\.test/script-url\.bin" });
        Assert.Single(items);
    }
}
