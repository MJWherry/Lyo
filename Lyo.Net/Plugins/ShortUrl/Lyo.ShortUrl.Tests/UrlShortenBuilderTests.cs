namespace Lyo.ShortUrl.Tests;

public class UrlShortenBuilderTests
{
    [Fact]
    public void Build_WithLongUrl_ReturnsCorrectValues()
    {
        var builder = new UrlShortenBuilder().SetLongUrl("https://example.com/test");
        var (longUrl, customAlias, expirationDate) = builder.Build();
        Assert.Equal("https://example.com/test", longUrl);
        Assert.Null(customAlias);
        Assert.Null(expirationDate);
    }

    [Fact]
    public void Build_WithAllProperties_ReturnsCorrectValues()
    {
        var expirationDate = DateTime.UtcNow.AddDays(7);
        var builder = new UrlShortenBuilder().SetLongUrl("https://example.com/test").SetCustomAlias("test-alias").SetExpirationDate(expirationDate);
        var (longUrl, customAlias, expirationDateResult) = builder.Build();
        Assert.Equal("https://example.com/test", longUrl);
        Assert.Equal("test-alias", customAlias);
        Assert.Equal(expirationDate.Date, expirationDateResult!.Value.Date);
    }

    [Fact]
    public void Build_WithNullLongUrl_ThrowsException()
    {
        var builder = new UrlShortenBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.Build());
    }

    [Fact]
    public void SetLongUrl_ChainsCorrectly()
    {
        var builder = new UrlShortenBuilder().SetLongUrl("https://example.com/test").SetCustomAlias("alias");
        var (longUrl, customAlias, _) = builder.Build();
        Assert.Equal("https://example.com/test", longUrl);
        Assert.Equal("alias", customAlias);
    }

    [Fact]
    public void SetCustomAlias_ChainsCorrectly()
    {
        var builder = new UrlShortenBuilder().SetLongUrl("https://example.com/test").SetCustomAlias("my-alias").SetExpirationDate(DateTime.UtcNow.AddDays(1));
        var (_, customAlias, expirationDate) = builder.Build();
        Assert.Equal("my-alias", customAlias);
        Assert.NotNull(expirationDate);
    }

    [Fact]
    public void SetExpirationDate_ChainsCorrectly()
    {
        var expirationDate = DateTime.UtcNow.AddDays(5);
        var builder = new UrlShortenBuilder().SetLongUrl("https://example.com/test").SetExpirationDate(expirationDate);
        var (_, _, expirationDateResult) = builder.Build();
        Assert.Equal(expirationDate.Date, expirationDateResult!.Value.Date);
    }
}