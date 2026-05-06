using System.Net;
using System.Text.Json;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Tests;

public sealed class AutomationPlanRunnerExtendedStepsTests
{
    [Fact]
    public void Deserialize_Polymorphic_NewSteps_ResolvesExpectedTypes()
    {
        const string json = """
                            {
                              "name": "new steps",
                              "steps": [
                                { "type": "httpRequest", "method": "POST", "url": "https://example.com", "bodyTemplate": "{\"x\":1}" },
                                { "type": "downloadFile", "url": "https://example.com/a.png", "targetFilePath": "/tmp/a.png" },
                                { "type": "extractSources", "elementsListRefName": "imgs", "variableName": "imgUrls" },
                                { "type": "upsertJsonRecords", "recordsJsonVariableName": "dtoJson", "targetName": "products" },
                                { "type": "uploadDirectoryToFileStorage", "sourceDirectory": "/tmp/files", "destinationPrefix": "dest" },
                                { "type": "storeStringListFromTemplate", "sourceVariableName": "in", "variableName": "out", "itemTemplate": "{strings.item}" },
                                { "type": "invokeDiMethod", "serviceType": "Lyo.Web.Automation.Tests.AutomationPlanRunnerExtendedStepsTests+ContextWriterScraper, Lyo.Web.Automation.Tests", "methodName": "Scrape" }
                              ]
                            }
                            """;

        var plan = JsonSerializer.Deserialize<AutomationPlan>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(plan);
        Assert.Collection(
            plan!.Steps,
            step => Assert.IsType<HttpRequestAutomationStep>(step),
            step => Assert.IsType<DownloadFileAutomationStep>(step),
            step => Assert.IsType<ExtractSourcesAutomationStep>(step),
            step => Assert.IsType<UpsertJsonRecordsAutomationStep>(step),
            step => Assert.IsType<UploadDirectoryToFileStorageAutomationStep>(step),
            step => Assert.IsType<StoreStringListFromTemplateAutomationStep>(step),
            step => Assert.IsType<InvokeDiMethodAutomationStep>(step));
    }

    [Fact]
    public async Task RunWithResultAsync_HttpRequestStep_StoresStatusAndResponseBody()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{\"ok\":true}") });
        using var httpClient = new HttpClient(handler);
        using var session = new FakeSession();
        var runner = new AutomationPlanRunner(httpClient, new NullSink(), new NullStorage(), new SimpleServiceProvider(new Dictionary<Type, object>()));

        var plan = AutomationPlanBuilder
            .New("http request")
            .StoreLiteral("productId", "42")
            .HttpRequest(
                method: "POST",
                url: "https://api.local/products",
                headers: new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                bodyTemplate: "{\"id\":\"{strings.productId}\"}",
                responseBodyVariableName: "responseJson",
                statusCodeVariableName: "statusCode")
            .Build();

        var result = await runner.RunWithResultAsync(
            session,
            plan,
            runtime: null,
            logger: null,
            ct: CancellationToken.None);

        Assert.Equal("202", result.Snapshot.Strings["statusCode"]);
        Assert.Equal("{\"ok\":true}", result.Snapshot.Strings["responseJson"]);
    }

    [Fact]
    public async Task RunWithResultAsync_InvokeDiMethod_WritesSharedContext_ForLaterTemplateUse()
    {
        using var session = new FakeSession();
        var sp = new SimpleServiceProvider(new Dictionary<Type, object> { [typeof(ContextWriterScraper)] = new ContextWriterScraper() });
        using var httpClient = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var runner = new AutomationPlanRunner(httpClient, new NullSink(), new NullStorage(), sp);

        var plan = AutomationPlanBuilder
            .New("context persist")
            .InvokeDiMethod(
                serviceType: typeof(ContextWriterScraper).AssemblyQualifiedName!,
                methodName: nameof(ContextWriterScraper.Scrape))
            .StoreTemplate("cookie", "{context.auth.cookie}")
            .Build();

        var result = await runner.RunWithResultAsync(
            session,
            plan,
            runtime: null,
            logger: null,
            ct: CancellationToken.None);

        Assert.Equal("session=abc", result.Snapshot.Strings["cookie"]);
        Assert.True(result.Context.Overall.TryGetContextValue("auth.cookie", out var value));
        Assert.Equal("session=abc", value?.ToString());
    }

    [Fact]
    public async Task RunWithResultAsync_InvokeDiMethod_MissingMethodCanBeNonThrowing()
    {
        using var session = new FakeSession();
        var sp = new SimpleServiceProvider(new Dictionary<Type, object> { [typeof(ContextWriterScraper)] = new ContextWriterScraper() });
        using var httpClient = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var runner = new AutomationPlanRunner(httpClient, new NullSink(), new NullStorage(), sp);
        var plan = AutomationPlanBuilder
            .New("missing method")
            .InvokeDiMethod(
                serviceType: typeof(ContextWriterScraper).AssemblyQualifiedName!,
                methodName: "DoesNotExist",
                throwOnMissingMethod: false)
            .StoreLiteral("ok", "true")
            .Build();

        var result = await runner.RunWithResultAsync(
            session,
            plan,
            runtime: null,
            logger: null,
            ct: CancellationToken.None);

        Assert.Equal("true", result.Snapshot.Strings["ok"]);
    }

    [Fact]
    public void AddWebAutomationPlanRunner_RegistersRunner()
    {
        var services = new ServiceCollection();
        services.AddWebAutomationPlanRunner();
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IAutomationPlanRunner>());
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class ContextWriterScraper
    {
        public Task Scrape(AutomationPlanStepContext context, CancellationToken ct)
        {
            context.ContextItems["auth.cookie"] = "session=abc";
            return Task.CompletedTask;
        }
    }

    private sealed class SimpleServiceProvider(Dictionary<Type, object> instances) : IServiceProvider
    {
        public object? GetService(Type serviceType) => instances.TryGetValue(serviceType, out var value) ? value : null;
    }

    private sealed class NullSink : IAutomationPlanDataSink
    {
        public Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NullStorage : IAutomationPlanFileStorage
    {
        public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private sealed class FakeSession : IWebAutomationSession
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public string? SessionDirectory => null;
        public IWebAutomationBrowser Browser { get; } = new FakeBrowser();
        public Task StartBrowserAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }

    private sealed class FakeBrowser : IWebAutomationBrowser
    {
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
        public Task<IReadOnlyList<IWebAutomationElement>?> GetElementsAsync(ElementLocatorChain chain, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<IWebAutomationElement>?>(null);
        public Task<string> GetPageSourceAsync(CancellationToken ct = default) => Task.FromResult(string.Empty);
        public Task<string> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult("https://example.com");
        public Task<string> GetTitleAsync(CancellationToken ct = default) => Task.FromResult("title");
        public Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default) => Task.FromResult(Array.Empty<byte>());
        public Task SetViewportSizeAsync(int width, int height, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeTabs : IWebAutomationTabs
    {
        public Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AutomationTabInfo>>(Array.Empty<AutomationTabInfo>());
        public Task<AutomationTabInfo> GetCurrentTabAsync(CancellationToken ct = default) => Task.FromResult(new AutomationTabInfo(0, true, "k", "about:blank", "tab"));
        public Task SwitchToTabAsync(int tabIndex, CancellationToken ct = default) => Task.CompletedTask;
        public Task SwitchToTabAsync(string tabKey, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string> OpenNewTabAsync(string? url = null, CancellationToken ct = default) => Task.FromResult("new-tab");
        public Task CloseCurrentTabAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task SetTabDisplayNameAsync(string tabKey, string? displayName, CancellationToken ct = default) => Task.CompletedTask;
    }
}
