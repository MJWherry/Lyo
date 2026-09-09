using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Web.Components;

namespace Lyo.Web.WebRenderer.Tests;

public sealed class WebRendererServiceTypeTests
{
    [Fact]
    public async Task RenderToHtmlAsync_GenericAndType_Match()
    {
        var renderer = CreateRenderer();
        var parameters = new Dictionary<string, object> { ["Name"] = "Ada" };
        var generic = await renderer.RenderToHtmlAsync<HelloRenderComponent>(parameters, TestContext.Current.CancellationToken);
        var byType = await renderer.RenderToHtmlAsync(typeof(HelloRenderComponent), parameters, TestContext.Current.CancellationToken);
        var byName = await renderer.RenderToHtmlAsync(typeof(HelloRenderComponent).FullName!, parameters, TestContext.Current.CancellationToken);
        Assert.Contains("Hello Ada", generic, StringComparison.Ordinal);
        Assert.Equal(generic, byType);
        Assert.Equal(generic, byName);
    }

    [Fact]
    public async Task RenderToHtmlAsync_ReportViewerGenericAndType_Match()
    {
        var renderer = CreateRenderer();
        var parameters = new Dictionary<string, object> { ["Report"] = new Report<object> { Title = "Quarterly" } };
        var generic = await renderer.RenderToHtmlAsync<ReportViewer<object>>(parameters, TestContext.Current.CancellationToken);
        var byType = await renderer.RenderToHtmlAsync(typeof(ReportViewer<object>), parameters, TestContext.Current.CancellationToken);
        Assert.Contains("Quarterly", generic, StringComparison.Ordinal);
        Assert.Equal(generic, byType);
    }

    [Fact]
    public async Task RenderToHtmlAsync_UnknownName_Throws()
    {
        var renderer = CreateRenderer();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => renderer.RenderToHtmlAsync("Lyo.Does.Not.Exist.Component", null, TestContext.Current.CancellationToken));
        Assert.Contains("Could not resolve", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToHtmlAsync_NonComponent_Throws()
    {
        var renderer = CreateRenderer();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => renderer.RenderToHtmlAsync(typeof(string), null, TestContext.Current.CancellationToken));
        Assert.Contains("IComponent", ex.Message, StringComparison.Ordinal);
    }

    private static WebRendererService CreateRenderer() => WebRendererTestHost.CreateService();
}
