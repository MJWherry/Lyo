namespace Lyo.Web.Automation.Studio.Tests;

public class StudioAddressTests
{
    [Fact]
    public void Normalize_AddsHttpsWhenTheHostHasNoScheme()
    {
        Assert.Equal("https://mangafire.to", StudioAddress.Normalize("mangafire.to"));
        Assert.Equal("https://mangafire.to/manga", StudioAddress.Normalize(" mangafire.to/manga "));
    }

    [Fact]
    public void Normalize_KeepsHttpHttpsAndAbout()
    {
        Assert.Equal("https://mangafire.to/", StudioAddress.Normalize("https://mangafire.to/"));
        Assert.Equal("http://localhost:8080", StudioAddress.Normalize("http://localhost:8080"));
        Assert.Equal("about:blank", StudioAddress.Normalize("about:blank"));
        Assert.Null(StudioAddress.Normalize("  "));
    }
}