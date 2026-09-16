using Lyo.Common.Core.Identifiers;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Tests;

public sealed class AutomationPlanRunControlTests
{
    [Fact]
    public async Task Run_Pause_HoldsBeforeTheNextStep()
    {
        var gate = new AutomationPlanRunGate();
        var started = new List<int>();
        using var session = new ControlSession();
        using var http = new HttpClient();
        var runner = new AutomationPlanRunner(http, new ControlSink(), new ControlStorage(), new ControlServices());
        var plan = new AutomationPlan("pause", [
            new StoreLiteralAutomationStep("a", "1"),
            new StoreLiteralAutomationStep("b", "2")
        ]);
        var options = new AutomationPlanRuntimeOptions {
            Gate = gate,
            Instrumentation = new StepIndexInstrumentation(started),
            Hooks = new() {
                AfterStepAsync = (ctx, _, _) => {
                    if (ctx.StepIndex == 0)
                        gate.Pause();

                    return ValueTask.CompletedTask;
                }
            }
        };

        var run = runner.RunWithResultAsync(session, plan, options, null, CancellationToken.None);
        var held = await WaitUntilAsync(() => started.Count == 1 && !run.IsCompleted, TimeSpan.FromSeconds(2));
        Assert.True(held, "the second step started while the run was paused");
        gate.Resume();
        var result = await run;
        Assert.Equal(["0", "1"], started.Select(i => i.ToString()));
        Assert.Equal("2", result.Snapshot.Strings["b"]);
    }

    [Fact]
    public async Task Run_StopWhilePaused_CancelsBeforeTheHeldStep()
    {
        var gate = new AutomationPlanRunGate();
        gate.Pause();
        var started = new List<int>();
        using var session = new ControlSession();
        using var http = new HttpClient();
        var runner = new AutomationPlanRunner(http, new ControlSink(), new ControlStorage(), new ControlServices());
        var plan = new AutomationPlan("stop", [new StoreLiteralAutomationStep("a", "1")]);
        var options = new AutomationPlanRuntimeOptions { Gate = gate, Instrumentation = new StepIndexInstrumentation(started) };
        using var cts = new CancellationTokenSource();

        var run = runner.RunWithResultAsync(session, plan, options, null, cts.Token);
        var waiting = await WaitUntilAsync(() => !run.IsCompleted, TimeSpan.FromSeconds(1));
        Assert.True(waiting);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Empty(started);
    }

    [Fact]
    public async Task Run_StartStepIndex_SkipsEarlierSteps()
    {
        var outcomes = new List<(int Index, AutomationPlanStepOutcome Outcome)>();
        using var session = new ControlSession();
        using var http = new HttpClient();
        var runner = new AutomationPlanRunner(http, new ControlSink(), new ControlStorage(), new ControlServices());
        var plan = new AutomationPlan("from here", [
            new StoreLiteralAutomationStep("a", "0"),
            new StoreLiteralAutomationStep("b", "1"),
            new StoreLiteralAutomationStep("c", "2"),
            new StoreLiteralAutomationStep("d", "3")
        ]);
        var options = new AutomationPlanRuntimeOptions { StartStepIndex = 2, Instrumentation = new OutcomeInstrumentation(outcomes) };

        var result = await runner.RunWithResultAsync(session, plan, options, null, CancellationToken.None);

        Assert.False(result.Snapshot.Strings.ContainsKey("a"));
        Assert.False(result.Snapshot.Strings.ContainsKey("b"));
        Assert.Equal("2", result.Snapshot.Strings["c"]);
        Assert.Equal("3", result.Snapshot.Strings["d"]);
        Assert.Equal(
            [(0, AutomationPlanStepOutcome.Skipped), (1, AutomationPlanStepOutcome.Skipped), (2, AutomationPlanStepOutcome.Success), (3, AutomationPlanStepOutcome.Success)],
            outcomes);
    }

    [Fact]
    public async Task Run_EndStepIndex_StopsAfterThatStep()
    {
        var outcomes = new List<(int Index, AutomationPlanStepOutcome Outcome)>();
        using var session = new ControlSession();
        using var http = new HttpClient();
        var runner = new AutomationPlanRunner(http, new ControlSink(), new ControlStorage(), new ControlServices());
        var plan = new AutomationPlan("one", [
            new StoreLiteralAutomationStep("a", "0"),
            new StoreLiteralAutomationStep("b", "1"),
            new StoreLiteralAutomationStep("c", "2"),
            new StoreLiteralAutomationStep("d", "3")
        ]);
        var options = new AutomationPlanRuntimeOptions { StartStepIndex = 2, EndStepIndex = 2, Instrumentation = new OutcomeInstrumentation(outcomes) };

        var result = await runner.RunWithResultAsync(session, plan, options, null, CancellationToken.None);

        Assert.Equal("2", result.Snapshot.Strings["c"]);
        Assert.False(result.Snapshot.Strings.ContainsKey("d"));
        Assert.Equal(
            [(0, AutomationPlanStepOutcome.Skipped), (1, AutomationPlanStepOutcome.Skipped), (2, AutomationPlanStepOutcome.Success)],
            outcomes);
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline) {
            if (condition())
                return true;

            await Task.Delay(20);
        }

        return condition();
    }

    private sealed class StepIndexInstrumentation(List<int> started) : AutomationPlanInstrumentationBase
    {
        public override void OnStepStarting(in AutomationPlanStepStartingEvent e) => started.Add(e.StepIndex);
    }

    private sealed class OutcomeInstrumentation(List<(int Index, AutomationPlanStepOutcome Outcome)> outcomes) : AutomationPlanInstrumentationBase
    {
        public override void OnStepOutcome(in AutomationPlanStepOutcomeRecord e) => outcomes.Add((e.StepIndex, e.Outcome));
    }

    private sealed class ControlSink : IAutomationPlanDataSink
    {
        public Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ControlStorage : IAutomationPlanFileStorage
    {
        public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> SaveSnapshotAsync(byte[] png, Guid runId, Guid stepId, CancellationToken ct) => Task.FromResult<string?>(null);

        public Task<string?> SaveAsync(byte[] data, string fileName, string pathPrefix, string contentType, CancellationToken ct) => Task.FromResult<string?>(null);

        public Task<byte[]?> ReadSnapshotAsync(string fileId, CancellationToken ct) => Task.FromResult<byte[]?>(null);
    }

    private sealed class ControlServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class ControlSession : IWebAutomationSession
    {
        public Guid SessionId { get; } = LyoGuid.CreateV7();

        public string? SessionDirectory => null;

        public IWebAutomationBrowser Browser { get; } = new ControlBrowser();

        public Task StartBrowserAsync(CancellationToken ct = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Dispose() { }
    }

    private sealed class ControlBrowser : IWebAutomationBrowser
    {
        public IBrowserCookies? CookieJar => null;

        public IBrowserHeaders? ExtraHeaders => null;

        public IWebAutomationNavigator Navigator => this;

        public IWebAutomationPage CurrentPage => this;

        public IWebAutomationTabs Tabs { get; } = new ControlTabs();

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

        public Task<string> GetPageSourceAsync(CancellationToken ct = default) => Task.FromResult(string.Empty);

        public Task<string> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult("https://example.com");

        public Task<string> GetTitleAsync(CancellationToken ct = default) => Task.FromResult("title");

        public Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default) => Task.FromResult(Array.Empty<byte>());

        public Task SetViewportSizeAsync(int width, int height, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class ControlTabs : IWebAutomationTabs
    {
        public Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AutomationTabInfo>>([]);

        public Task<AutomationTabInfo> GetCurrentTabAsync(CancellationToken ct = default) => Task.FromResult(new AutomationTabInfo(0, true, "k", "about:blank", "tab"));

        public Task SwitchToTabAsync(int tabIndex, CancellationToken ct = default) => Task.CompletedTask;

        public Task SwitchToTabAsync(string tabKey, CancellationToken ct = default) => Task.CompletedTask;

        public Task<string> OpenNewTabAsync(string? url = null, CancellationToken ct = default) => Task.FromResult("new-tab");

        public Task CloseCurrentTabAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task SetTabDisplayNameAsync(string tabKey, string? displayName, CancellationToken ct = default) => Task.CompletedTask;
    }
}
