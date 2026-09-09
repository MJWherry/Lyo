using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Tests;

public sealed class RandomizeViewportPlanTests
{
    [Fact]
    public void Deserialize_RandomizeViewport_ResolvesType()
    {
        const string json = """
                            {
                              "name": "resize",
                              "steps": [
                                { "type": "setViewportSize", "width": 1440, "height": 900 },
                                { "type": "randomizeViewport", "jitterPixels": 4, "sizes": [ { "width": 1920, "height": 1080 } ] }
                              ]
                            }
                            """;

        var plan = System.Text.Json.JsonSerializer.Deserialize<AutomationPlan>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(plan);
        Assert.IsType<SetViewportSizeAutomationStep>(plan.Steps[0]);
        var step = Assert.IsType<RandomizeViewportAutomationStep>(plan.Steps[1]);
        Assert.Equal(4, step.JitterPixels);
        Assert.Equal(new DesktopDisplaySize(1920, 1080), Assert.Single(step.Sizes!));
    }

    [Fact]
    public async Task Run_RandomizeViewport_UsesProvidedPool()
    {
        using var session = new RecordingSession();
        using var httpClient = new HttpClient(new StubHandler());
        var runner = new AutomationPlanRunner(httpClient, new NullSink(), new NullStorage(), new EmptyServices());
        var plan = AutomationPlanBuilder.New()
            .ResizeWindow(1280, 800)
            .RandomizeViewport([new DesktopDisplaySize(1600, 900)])
            .Build();

        await runner.RunWithResultAsync(session, plan, null, null, CancellationToken.None);
        Assert.Equal((1280, 800), session.Browser.Sizes[0]);
        Assert.Equal((1600, 900), session.Browser.Sizes[1]);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    private sealed class NullSink : IAutomationPlanDataSink
    {
        public Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NullStorage : IAutomationPlanFileStorage
    {
        public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class EmptyServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class RecordingSession : IWebAutomationSession
    {
        public Guid SessionId { get; } = Guid.NewGuid();

        public string? SessionDirectory => null;

        public RecordingBrowser Browser { get; } = new();

        IWebAutomationBrowser IWebAutomationSession.Browser => Browser;

        public Task StartBrowserAsync(CancellationToken ct = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Dispose() { }
    }

    private sealed class RecordingBrowser : IWebAutomationBrowser
    {
        public List<(int Width, int Height)> Sizes { get; } = [];

        public IBrowserCookies? CookieJar => null;

        public IBrowserHeaders? ExtraHeaders => null;

        public IWebAutomationNavigator Navigator => this;

        public IWebAutomationPage CurrentPage => this;

        public IWebAutomationTabs Tabs { get; } = new FakeTabs();

        public Task NavigateAsync(string url, CancellationToken ct = default) => Task.CompletedTask;

        public Task NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct = default) => Task.CompletedTask;

        public Task ReloadAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IWebAutomationElement> PollForElementAsync(ElementLocatorChain chain, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IWebAutomationElement?> GetElementAsync(ElementLocatorChain chain, CancellationToken ct = default) => Task.FromResult<IWebAutomationElement?>(null);

        public Task<IReadOnlyList<IWebAutomationElement>?> GetElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IWebAutomationElement>?>(null);

        public Task<string> GetPageSourceAsync(CancellationToken ct = default) => Task.FromResult(string.Empty);

        public Task<string> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult("https://example.com");

        public Task<string> GetTitleAsync(CancellationToken ct = default) => Task.FromResult("title");

        public Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default) => Task.FromResult(Array.Empty<byte>());

        public Task SetViewportSizeAsync(int width, int height, CancellationToken ct = default)
        {
            Sizes.Add((width, height));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTabs : IWebAutomationTabs
    {
        public Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutomationTabInfo>>([]);

        public Task<AutomationTabInfo> GetCurrentTabAsync(CancellationToken ct = default) => Task.FromResult(new AutomationTabInfo(0, true, "k", "about:blank", "tab"));

        public Task SwitchToTabAsync(int tabIndex, CancellationToken ct = default) => Task.CompletedTask;

        public Task SwitchToTabAsync(string tabKey, CancellationToken ct = default) => Task.CompletedTask;

        public Task<string> OpenNewTabAsync(string? url = null, CancellationToken ct = default) => Task.FromResult("new-tab");

        public Task CloseCurrentTabAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task SetTabDisplayNameAsync(string tabKey, string? displayName, CancellationToken ct = default) => Task.CompletedTask;
    }
}
