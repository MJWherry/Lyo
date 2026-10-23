using Lyo.Web.Components.Catalog;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Tests;

public sealed class CatalogAssetUriTests
{
    [Fact]
    public void Resolve_RelativeCatalogPath_UsesTheHostOrigin()
    {
        var navigation = new TestNavigation("https://gateway.test/app/");
        var uri = CatalogAssetUri.Resolve(navigation, "_content/Lyo.Web.Components/catalog/index.json");
        Assert.Equal("https://gateway.test/app/_content/Lyo.Web.Components/catalog/index.json", uri.AbsoluteUri);
    }

    [Fact]
    public void Resolve_AbsoluteUrl_IsLeftAlone()
    {
        var navigation = new TestNavigation("https://gateway.test/");
        var uri = CatalogAssetUri.Resolve(navigation, "https://cdn.test/catalog/index.json");
        Assert.Equal("https://cdn.test/catalog/index.json", uri.AbsoluteUri);
    }

    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation(string baseUri) => Initialize(baseUri, baseUri);

        protected override void NavigateToCore(string uri, NavigationOptions options) { }
    }
}