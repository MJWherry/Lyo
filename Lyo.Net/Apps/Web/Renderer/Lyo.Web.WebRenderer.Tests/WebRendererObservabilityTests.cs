using System.Text;
using Lyo.Metrics;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer.Tests;

public sealed class WebRendererObservabilityTests
{
    [Fact]
    public async Task RenderToHtmlAsync_AsyncInit_ReturnsHtml()
    {
        var renderer = CreateRenderer();
        var html = await renderer.RenderToHtmlAsync<AsyncHelloRenderComponent>(new() { ["Name"] = "Ada" }, TestCt);
        Assert.Contains("Hello Ada!", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToHtmlBytesAsync_RecordsBytesCounterOnce()
    {
        var metrics = new RecordingMetrics();
        var renderer = CreateRenderer(metrics: metrics, options: new() { EnableMetrics = true });
        var bytes = await renderer.RenderToHtmlBytesAsync<HelloRenderComponent>(new() { ["Name"] = "Ada" }, TestCt);
        Assert.Contains("Hello Ada", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.Contains(metrics.Calls, call => call.Kind == "counter" && call.Name == Constants.Metrics.RenderToHtmlBytesSuccess);
        Assert.DoesNotContain(metrics.Calls, call => call.Name == Constants.Metrics.RenderToHtmlSuccess);
    }

    [Fact]
    public async Task RenderToHtmlAsync_Failure_LogsWarning()
    {
        var logger = new ListLogger<WebRendererService>();
        var renderer = CreateRenderer(logger);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => renderer.RenderToHtmlAsync(typeof(string), null, TestCt));
        Assert.Contains("IComponent", ex.Message, StringComparison.Ordinal);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task ComponentRendered_HandlerThrows_StillReturnsHtml()
    {
        var logger = new ListLogger<WebRendererService>();
        var renderer = CreateRenderer(logger);
        renderer.ComponentRendered += (_, _) => throw new InvalidOperationException("subscriber failed");
        var html = await renderer.RenderToHtmlAsync<HelloRenderComponent>(new() { ["Name"] = "Ada" }, TestCt);
        Assert.Contains("Hello Ada", html, StringComparison.Ordinal);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Exception is InvalidOperationException);
    }

    private static WebRendererService CreateRenderer(ILogger<WebRendererService>? logger = null, IMetrics? metrics = null, WebRenderOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var htmlRenderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return new(htmlRenderer, logger, metrics, options);
    }
}