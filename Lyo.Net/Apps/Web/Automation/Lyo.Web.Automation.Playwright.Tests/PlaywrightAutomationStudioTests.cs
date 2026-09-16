using Lyo.Web.Automation;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright.Configuration;
using Lyo.Web.Automation.Playwright.Record;
using Lyo.Web.Automation.Playwright.Service;
using Lyo.Web.Automation.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class PlaywrightAutomationStudioTests : IClassFixture<WebAutomationTestPageHostFixture>, IDisposable
{
    private readonly WebAutomationTestPageHostFixture _pageHost;
    private readonly ServiceProvider _provider;

    public PlaywrightAutomationStudioTests(WebAutomationTestPageHostFixture pageHost)
    {
        _pageHost = pageHost;
        var root = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", "playwright-recorder");
        Directory.CreateDirectory(root);
        var services = new ServiceCollection();
        services.AddPlaywrightBrowserService(options => {
            options.Headless = true;
            options.ServiceRootDirectory = root;
            options.EnableMetrics = false;
            options.BrowserKind = AutomationBrowserKind.Chromium;
            options.NavigationTimeoutMs = 20_000;
            options.LocatorDefaultTimeoutMs = 20_000;
            options.ViewportWidth = 1280;
            options.ViewportHeight = 900;
        });
        services.AddWebAutomationPlanRunner();
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    private CancellationToken TestCt => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Session_Start_DoesNotInjectAScript_AndAClickDoesNotChangeThePlan()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), PlanName = "controls", Headless = true }, ct);
        var page = session.Browser.Page!;
        await page.Locator("#searchButton").WaitForAsync(new() { Timeout = 20_000 });
        var injected = await page.EvaluateAsync<bool>("() => window.__lyoRecorderInstalled === true || typeof window.__lyoRecord === 'function' || !!window.__lyoTool");
        Assert.False(injected);
        Assert.Equal(0, await page.Locator("#lyo-recorder-host").CountAsync());
        await page.ClickAsync("#searchButton");
        await Task.Delay(200, ct);
        Assert.Empty(session.Plan.Steps);
    }

    [Fact]
    public async Task Session_Record_ClickAddsAClickStep()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), PlanName = "controls", Headless = true }, ct);
        var page = session.Browser.Page!;
        await page.Locator("#searchButton").WaitForAsync(new() { Timeout = 20_000 });
        Assert.Empty(session.Plan.Steps);

        var recorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.PlanChanged += (_, _) => recorded.TrySetResult();
        await session.StartRecordingAsync(ct);
        await page.ClickAsync("#searchButton");
        await recorded.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);

        Assert.Contains(session.Plan.Steps, step => step is FindAndActAutomationStep act && act.Action is ClickElementAction);
    }

    [Fact]
    public async Task Session_Record_ReadingThePlanWhileTyping_KeepsOneInputStep()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), PlanName = "controls", Headless = true }, ct);
        var page = session.Browser.Page!;
        await page.Locator("#searchInput").WaitForAsync(new() { Timeout = 20_000 });
        var typed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var most = 0;
        session.PlanChanged += (_, _) => {
            var inputs = session.Plan.Steps.Count(step => step is FindAndActAutomationStep act && act.Action is InputTextElementAction);
            if (inputs > most)
                most = inputs;
            if (session.Plan.Steps.Any(step => step is FindAndActAutomationStep act && act.Action is InputTextElementAction input && input.Text == "comics"))
                typed.TrySetResult();
        };

        await session.StartRecordingAsync(ct);
        await page.Locator("#searchInput").ClickAsync();
        await page.Locator("#searchInput").PressSequentiallyAsync("comics", new() { Delay = 30 });
        await typed.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        var left = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.PlanChanged += (_, _) => {
            if (session.Plan.Steps.Any(step => step is FindAndActAutomationStep act && act.Action is ClickElementAction && act.Locator.Value == "searchButton"))
                left.TrySetResult();
        };
        await page.Locator("#searchButton").ClickAsync();
        await left.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);

        var text = session.Plan.Steps
            .OfType<FindAndActAutomationStep>()
            .Select(step => step.Action)
            .OfType<InputTextElementAction>()
            .Select(input => input.Text)
            .ToArray();
        Assert.Equal(["comics"], text);
        Assert.Equal(1, most);
    }

    [Fact]
    public async Task Session_Record_NavigationAddsANavigateStep()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), PlanName = "controls", Headless = true }, ct);
        var page = session.Browser.Page!;
        await page.Locator("#searchButton").WaitForAsync(new() { Timeout = 20_000 });
        var recorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.PlanChanged += (_, _) => recorded.TrySetResult();
        await session.StartRecordingAsync(ct);

        await page.GotoAsync(_pageHost.NextUri.ToString());
        await recorded.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);

        Assert.Contains(session.Plan.Steps, step => step is NavigateAutomationStep navigate && navigate.Url == _pageHost.NextUri.ToString());
    }

    [Fact]
    public async Task Session_Play_RunsTheLoadedPlan()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), Headless = true }, ct);
        var notices = new List<AutomationStudioStepNotice>();
        session.StepNotice += (_, notice) => notices.Add(notice);
        var directory = Path.Combine(Path.GetTempPath(), "lyo-studio-play", Guid.NewGuid().ToString("N"));
        var plan = new AutomationPlan("play", [new StorePageTitleAutomationStep("title")]);

        var result = await session.PlayAsync(plan, 0, directory, ct);

        Assert.Contains(notices, notice => notice.Phase == "start" && notice.StepIndex == 0);
        var outcome = Assert.Single(notices, notice => notice.Phase == "outcome");
        Assert.Equal(AutomationPlanStepOutcome.Success, outcome.Outcome);
        Assert.NotNull(outcome.Context);
        Assert.False(string.IsNullOrWhiteSpace(outcome.ScreenshotPath));
        Assert.True(File.Exists(outcome.ScreenshotPath));
        Assert.Contains(notices, notice => notice.Phase == "log" && !string.IsNullOrWhiteSpace(notice.LogLine));
        Assert.False(string.IsNullOrWhiteSpace(result.Snapshot.Strings["title"]));
    }

    [Fact]
    public async Task Session_Stop_CancelsAnActiveRun()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), Headless = true }, ct);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.StepNotice += (_, notice) => {
            if (notice.Phase == "start")
                started.TrySetResult();
        };
        var directory = Path.Combine(Path.GetTempPath(), "lyo-studio-stop", Guid.NewGuid().ToString("N"));
        var plan = new AutomationPlan("stop", [new DelayAutomationStep(20_000)]);
        var play = session.PlayAsync(plan, 0, directory, ct);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        session.CancelPlay();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => play);
        watch.Stop();
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"stop left the wait running for {watch.Elapsed}");
    }

    [Fact]
    public async Task Session_NetworkAndGrabber_ListARequestAndDescribeThePage()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), Headless = true }, ct);
        var page = session.Browser.Page!;
        await page.Locator("body").WaitForAsync(new() { Timeout = 20_000 });
        session.StartNetworkCapture();
        await page.EvaluateAsync("url => fetch(url)", _pageHost.ControlsUri.ToString());
        AutomationStudioNetworkRow? row = null;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline && row == null) {
            row = session.NetworkRows(_pageHost.ControlsUri.Host).FirstOrDefault();
            if (row == null)
                await Task.Delay(50, ct);
        }

        Assert.NotNull(row);
        Assert.Equal("fetch", row.ResourceType);
        var detail = session.ReadNetwork(row.Id);
        Assert.NotNull(detail);
        Assert.Contains(_pageHost.ControlsUri.Host, detail.Url, StringComparison.OrdinalIgnoreCase);
        await session.SetGrabberAsync(true, ct);
        await page.HoverAsync("#searchButton");
        var info = await session.DescribeElementAsync(ct);
        Assert.False(string.IsNullOrWhiteSpace(info?.Tag));
        Assert.False(string.IsNullOrWhiteSpace(info?.Url));
        Assert.False(string.IsNullOrWhiteSpace(info?.Locator?.Value));
        await session.HighlightAsync(new(ElementLocatorKind.Id, "searchButton"), ct);
        var marked = await page.Locator("#searchButton").EvaluateAsync<bool>("el => el.dataset.lyoTarget === '1'");
        Assert.True(marked);
        var injected = await page.EvaluateAsync<bool>("() => window.__lyoRecorderInstalled === true || typeof window.__lyoRecord === 'function'");
        Assert.False(injected);
    }

    [Fact]
    public async Task Session_Grabber_LongLink_InsertsACssLocator()
    {
        var ct = TestCt;
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        await using var session = await studio.StartAsync(new() { Headless = true, PlanName = "links" }, ct);
        var page = session.Browser.Page!;
        const string html = """
            <main>
              <a class="card" href="#">Manhua
            Ch. 1653d ago All-Class Awakening: God Slayer and a paragraph that is longer than a label</a>
              <a class="next" href="#">Next</a>
            </main>
            """;
        await page.GotoAsync("data:text/html," + Uri.EscapeDataString(html), new() { Timeout = 20_000, WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded });
        await session.SetGrabberAsync(true, ct);
        await page.Locator("a.card").HoverAsync();
        var cardError = await session.InsertGrabbedAsync("text", "card", ct);
        await page.Locator("a.next").HoverAsync();
        var nextError = await session.InsertGrabbedAsync("text", "next", ct);
        Assert.Null(cardError);
        Assert.Null(nextError);
        var card = Assert.IsType<FindElementAutomationStep>(session.Plan.Steps[0]);
        var next = Assert.IsType<FindElementAutomationStep>(session.Plan.Steps[2]);
        Assert.Equal(ElementLocatorKind.CssSelector, card.Locator.Kind);
        Assert.DoesNotContain("Manhua", card.Locator.Value, StringComparison.Ordinal);
        Assert.Equal(ElementLocatorKind.LinkText, next.Locator.Kind);
        Assert.Equal("Next", next.Locator.Value);
    }

    [Fact]
    public async Task Session_Grabber_PointerLeavesPage_ClearsOutlineAndDescribe()
    {
        var ct = TestCt;
        await using var session = await WatchingAsync(ct);
        var page = session.Browser.Page!;
        await page.HoverAsync("#searchButton");
        await session.HighlightAsync(new(ElementLocatorKind.Id, "searchButton"), ct);
        await page.EvaluateAsync("() => document.documentElement.dispatchEvent(new MouseEvent('mouseleave', { bubbles: false, relatedTarget: null }))");

        var marked = await page.Locator("#searchButton").EvaluateAsync<string>("el => (el.dataset.lyoTarget || '') + '|' + (el.style.outline || '')");
        Assert.Equal("|", marked);
        Assert.Null(await session.DescribeElementAsync(ct));
    }

    [Fact]
    public async Task Session_Grabber_HighlightWhilePointerOutside_DoesNotPaint()
    {
        var ct = TestCt;
        await using var session = await WatchingAsync(ct);
        var page = session.Browser.Page!;
        await page.HoverAsync("#searchButton");
        await page.EvaluateAsync("() => document.documentElement.dispatchEvent(new MouseEvent('mouseleave', { bubbles: false, relatedTarget: null }))");
        await session.HighlightAsync(new(ElementLocatorKind.Id, "searchButton"), ct);

        var marked = await page.Locator("#searchButton").EvaluateAsync<bool>("el => el.dataset.lyoTarget === '1'");
        Assert.False(marked);
    }

    [Fact]
    public async Task Session_Grabber_PointerReenters_HighlightsOnlyTheElementUnderTheCursor()
    {
        var ct = TestCt;
        await using var session = await WatchingAsync(ct);
        var page = session.Browser.Page!;
        await page.HoverAsync("#searchButton");
        await session.HighlightAsync(new(ElementLocatorKind.Id, "searchButton"), ct);
        await page.EvaluateAsync("() => document.documentElement.dispatchEvent(new MouseEvent('mouseleave', { bubbles: false, relatedTarget: null }))");
        await page.HoverAsync("#searchInput");

        var button = await page.Locator("#searchButton").EvaluateAsync<string>("el => (el.dataset.lyoTarget || '') + '|' + (el.style.outline || '')");
        var input = await page.Locator("#searchInput").EvaluateAsync<string>("el => el.style.outline || ''");
        Assert.Equal("|", button);
        Assert.Contains("solid", input, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Session_Grabber_MoveOffHighlightedElement_ClearsItAndIgnoresAStaleHighlight()
    {
        var ct = TestCt;
        await using var session = await WatchingAsync(ct);
        var page = session.Browser.Page!;
        await page.HoverAsync("#searchButton");
        await session.HighlightAsync(new(ElementLocatorKind.Id, "searchButton"), ct);
        await page.HoverAsync("#searchInput");

        var left = await page.Locator("#searchButton").EvaluateAsync<string>("el => (el.dataset.lyoTarget || '') + '|' + (el.style.outline || '')");
        Assert.Equal("|", left);

        await session.HighlightAsync(new(ElementLocatorKind.Id, "searchButton"), ct);
        left = await page.Locator("#searchButton").EvaluateAsync<string>("el => (el.dataset.lyoTarget || '') + '|' + (el.style.outline || '')");
        var current = await page.Locator("#searchInput").EvaluateAsync<string>("el => el.style.outline || ''");
        Assert.Equal("|", left);
        Assert.Contains("solid", current, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IPlaywrightAutomationStudio> WatchingAsync(CancellationToken ct)
    {
        var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
        var session = await studio.StartAsync(new() { StartUrl = _pageHost.ControlsUri.ToString(), Headless = true }, ct);
        await session.Browser.Page!.Locator("#searchButton").WaitForAsync(new() { Timeout = 20_000 });
        await session.SetGrabberAsync(true, ct);
        return session;
    }
}
