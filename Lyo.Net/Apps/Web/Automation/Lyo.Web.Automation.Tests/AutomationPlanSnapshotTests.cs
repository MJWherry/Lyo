using Lyo.Common.Core.Identifiers;
using Lyo.Testing;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Tests;

public sealed class AutomationPlanSnapshotTests
{
    [Fact]
    public async Task RunWithResultAsync_StoreLiteral_UploadsSnapshotAndReportsContext()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var storage = new MemoryStorage();
        var outcomes = new List<AutomationPlanStepOutcomeRecord>();
        using var session = new SnapshotSession(png);
        using var http = new HttpClient(new OkHandler());
        var runner = new AutomationPlanRunner(http, new NullSink(), storage, EmptyServices.Instance);
        var root = NewRoot();
        var plan = new AutomationPlan("greet", [new StoreLiteralAutomationStep("greeting", "hello")]);
        var runtime = new AutomationPlanRuntimeOptions {
            PlanRunDirectory = new AutomationPlanRunDirectoryOptions { RootDirectory = root },
            Instrumentation = new CollectingInstrumentation(outcomes)
        };

        var result = await runner.RunWithResultAsync(session, plan, runtime, null, TestCt);

        var outcome = Assert.Single(outcomes);
        Assert.Equal(storage.LastId, outcome.ScreenshotFileId);
        Assert.Equal(outcome.RunId, storage.LastRunId);
        Assert.Equal(outcome.StepExecutionId, storage.LastStepId);
        Assert.Equal(png, storage.LastBytes);
        Assert.NotNull(outcome.Context);
        Assert.Equal("hello", outcome.Context.Strings["greeting"]);
        Assert.Equal("hello", result.Snapshot.Strings["greeting"]);
        Assert.Equal(1, result.Context.StepCount);
        Directory.Delete(root, true);
    }

    [Fact]
    public async Task RunWithResultAsync_FailingStep_UploadsSnapshotAndKeepsFrame()
    {
        var storage = new MemoryStorage();
        var outcomes = new List<AutomationPlanStepOutcomeRecord>();
        AutomationPlanExecutionContext? failed = null;
        using var session = new SnapshotSession([9, 8, 7]);
        using var http = new HttpClient(new OkHandler());
        var runner = new AutomationPlanRunner(http, new NullSink(), storage, EmptyServices.Instance);
        var root = NewRoot();
        var plan = new AutomationPlan("fail", [
            new StoreLiteralAutomationStep("greeting", "hello"),
            new CaptureNetworkAutomationStep("urls", "(", 10)
        ]);
        var runtime = new AutomationPlanRuntimeOptions {
            PlanRunDirectory = new AutomationPlanRunDirectoryOptions { RootDirectory = root },
            Instrumentation = new CollectingInstrumentation(outcomes),
            Hooks = new AutomationPlanHooks {
                OnFailureAsync = (ctx, _) => {
                    failed = ctx.ExecutionContext;
                    return ValueTask.CompletedTask;
                }
            }
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunWithResultAsync(session, plan, runtime, null, TestCt));

        Assert.Contains("(", error.Message);
        Assert.Equal(2, outcomes.Count);
        var failedOutcome = outcomes[1];
        Assert.Equal(AutomationPlanStepOutcome.Failed, failedOutcome.Outcome);
        Assert.Equal(storage.LastId, failedOutcome.ScreenshotFileId);
        Assert.Equal(failedOutcome.RunId, storage.LastRunId);
        Assert.Equal(failedOutcome.StepExecutionId, storage.LastStepId);
        Assert.NotNull(failedOutcome.Context);
        Assert.Equal("hello", failedOutcome.Context.Strings["greeting"]);
        Assert.NotNull(failed);
        Assert.Equal(2, failed.StepCount);
        Assert.Equal("hello", failed[1].Strings["greeting"]);
        Directory.Delete(root, true);
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-automation-snapshot-tests", LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class CollectingInstrumentation(List<AutomationPlanStepOutcomeRecord> outcomes) : AutomationPlanInstrumentationBase
    {
        public override void OnStepOutcome(in AutomationPlanStepOutcomeRecord e) => outcomes.Add(e);
    }

    private sealed class MemoryStorage : IAutomationPlanFileStorage
    {
        private int _next = 1;

        public string? LastId { get; private set; }

        public byte[]? LastBytes { get; private set; }

        public Guid LastRunId { get; private set; }

        public Guid LastStepId { get; private set; }

        public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> SaveSnapshotAsync(byte[] png, Guid runId, Guid stepId, CancellationToken ct)
        {
            LastBytes = png;
            LastRunId = runId;
            LastStepId = stepId;
            LastId = "file-" + _next++;
            return Task.FromResult<string?>(LastId);
        }

        public Task<string?> SaveAsync(byte[] data, string fileName, string pathPrefix, string contentType, CancellationToken ct) => Task.FromResult<string?>(null);

        public Task<byte[]?> ReadSnapshotAsync(string fileId, CancellationToken ct) => Task.FromResult<byte[]?>(LastBytes);
    }

    private sealed class NullSink : IAutomationPlanDataSink
    {
        public Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class EmptyServices : IServiceProvider
    {
        public static readonly EmptyServices Instance = new();

        public object? GetService(Type serviceType) => null;
    }

    private sealed class OkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    private sealed class SnapshotSession(byte[] png) : IWebAutomationSession
    {
        public Guid SessionId { get; } = LyoGuid.CreateV7();

        public string? SessionDirectory => null;

        public IWebAutomationBrowser Browser { get; } = new SnapshotBrowser(png);

        public Task StartBrowserAsync(CancellationToken ct = default) => Task.CompletedTask;

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class SnapshotBrowser(byte[] png) : IWebAutomationBrowser
    {
        public IBrowserCookies? CookieJar => null;

        public IBrowserHeaders? ExtraHeaders => null;

        public IWebAutomationNavigator Navigator => this;

        public IWebAutomationPage CurrentPage => this;

        public IWebAutomationTabs Tabs { get; } = new SnapshotTabs();

        public Task NavigateAsync(string url, CancellationToken ct = default) => Task.CompletedTask;

        public Task NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct = default) => Task.CompletedTask;

        public Task ReloadAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> CaptureNetworkAsync(string? urlPattern, int durationMilliseconds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IWebAutomationElement> PollForElementAsync(ElementLocatorChain chain, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IWebAutomationElement?> GetElementAsync(ElementLocatorChain chain, CancellationToken ct = default) => Task.FromResult<IWebAutomationElement?>(null);

        public Task<IReadOnlyList<IWebAutomationElement>?> GetElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IWebAutomationElement>?>(null);

        public Task<string> GetPageSourceAsync(CancellationToken ct = default) => Task.FromResult("");

        public Task<string> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult("https://example.com");

        public Task<string> GetTitleAsync(CancellationToken ct = default) => Task.FromResult("title");

        public Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default) => Task.FromResult(png);

        public Task SetViewportSizeAsync(int width, int height, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class SnapshotTabs : IWebAutomationTabs
    {
        public Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AutomationTabInfo>>([]);

        public Task<AutomationTabInfo> GetCurrentTabAsync(CancellationToken ct = default) => Task.FromResult(new AutomationTabInfo(0, true, "k", "about:blank", "tab"));

        public Task SwitchToTabAsync(int tabIndex, CancellationToken ct = default) => Task.CompletedTask;

        public Task SwitchToTabAsync(string tabKey, CancellationToken ct = default) => Task.CompletedTask;

        public Task<string> OpenNewTabAsync(string? url = null, CancellationToken ct = default) => Task.FromResult("new");

        public Task CloseCurrentTabAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task SetTabDisplayNameAsync(string tabKey, string? displayName, CancellationToken ct = default) => Task.CompletedTask;
    }
}
