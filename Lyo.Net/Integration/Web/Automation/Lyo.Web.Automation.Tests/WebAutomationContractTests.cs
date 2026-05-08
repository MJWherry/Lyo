using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Tests;

public abstract class WebAutomationContractTests<TFactory>(TFactory factory, WebAutomationTestPageHostFixture pageHost)
    where TFactory : class, IWebAutomationTestEngineFactory
{
    [Fact]
    public async Task NavigateToHomePage_ReportsExpectedTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await session.StartBrowserAsync(ct);
        await session.Browser.NavigateAsync(pageHost.HomeUri.ToString(), ct);
        var title = await session.Browser.GetTitleAsync(ct);
        Assert.Equal("Web Automation Test Page", title);
    }

    [Fact]
    public async Task InputField_SendKeys_UpdatesEchoText()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await session.StartBrowserAsync(ct);
        await session.Browser.NavigateAsync(pageHost.HomeUri.ToString(), ct);
        var input = await session.Browser.PollForElementAsync(new(ElementLocator.Id("nameInput")), ct);
        await input.SendKeysAsync("Lyo", true, ct);
        var echo = await session.Browser.PollForElementAsync(new(ElementLocator.Id("nameEcho")), ct);
        var value = await echo.GetTextAsync(ct);
        Assert.Equal("Lyo", value);
    }

    [Fact]
    public async Task IncrementButton_Click_ChangesCounterText()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await session.StartBrowserAsync(ct);
        await session.Browser.NavigateAsync(pageHost.HomeUri.ToString(), ct);
        var button = await session.Browser.PollForElementAsync(new(ElementLocator.Id("incrementBtn")), ct);
        await button.ClickAsync(ct);
        var counter = await session.Browser.PollForElementAsync(new(ElementLocator.Id("counterText")), ct);
        var text = await counter.GetTextAsync(ct);
        Assert.Equal("1", text.Trim());
    }

    [Fact]
    public async Task Navigation_ClickLink_GoesToNextPage()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await session.StartBrowserAsync(ct);
        await session.Browser.NavigateAsync(pageHost.HomeUri.ToString(), ct);
        var link = await session.Browser.PollForElementAsync(new(ElementLocator.Id("nextPageLink")), ct);
        await link.ClickAsync(ct);
        var heading = await session.Browser.PollForElementAsync(new(ElementLocator.Id("nextHeadline")), ct);
        var headingText = await heading.GetTextAsync(ct);
        var url = await session.Browser.GetCurrentUrlAsync(ct);
        Assert.Equal("Web Automation Next Page", headingText.Trim());
        Assert.Contains("/next", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ControlsSearch_InputAndClick_UpdatesEcho()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await session.StartBrowserAsync(ct);
        await session.Browser.NavigateAsync(pageHost.ControlsUri.ToString(), ct);
        var searchInput = await session.Browser.PollForElementAsync(new(ElementLocator.Name("searchInput")), ct);
        await searchInput.SendKeysAsync("comics", true, ct);
        var searchButton = await session.Browser.PollForElementAsync(new(ElementLocator.Id("searchButton")), ct);
        await searchButton.ClickAsync(ct);
        var echo = await session.Browser.PollForElementAsync(new(ElementLocator.Id("searchEcho")), ct);
        var echoText = await echo.GetTextAsync(ct);
        Assert.Equal("search:comics", echoText.Trim());
    }

    [Fact]
    public async Task ControlsSearch_FindElements_ReturnsExpectedResults()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await session.StartBrowserAsync(ct);
        await session.Browser.NavigateAsync(pageHost.ControlsUri.ToString(), ct);
        var items = await session.Browser.PollForElementsAsync(new(ElementLocator.CssSelector(".search-item")), ct);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task AutomationPlan_StorePageTitle_StepStoresTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New("store page title").Navigate(pageHost.HomeUri.ToString()).StorePageTitle("title").Build();
        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Equal("Web Automation Test Page", result.Snapshot.Strings["title"]);
    }

    [Fact]
    public async Task AutomationPlan_StorePageUrl_StepStoresUrl()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New().Navigate(pageHost.ControlsUri.ToString()).StorePageUrl("url").Build();
        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Contains("/controls", result.Snapshot.Strings["url"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AutomationPlan_HttpRequest_StepStoresStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        var jsonPath = GetAssetPath("Data/http-request-body.json");
        await using var provider = CreatePlanRunnerProvider(s => s.AddSingleton<JsonPayloadService>());
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New()
            .InvokeDiMethod(typeof(JsonPayloadService).AssemblyQualifiedName!, nameof(JsonPayloadService.ReadJson), new() { ["path"] = jsonPath }, resultVariableName: "payload")
            .HttpRequest("POST", pageHost.EchoApiUri.ToString(), bodyTemplate: "{strings.payload}", statusCodeVariableName: "status")
            .Build();

        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Equal("202", result.Snapshot.Strings["status"]);
    }

    [Fact]
    public async Task AutomationPlan_DownloadFile_StepWritesFile()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        var tempPath = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", $"{Guid.NewGuid():N}.txt");
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New().DownloadFile(new Uri(pageHost.BaseUri, "files/sample-a.txt").ToString(), tempPath, "savedPath").Build();
        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.True(File.Exists(result.Snapshot.Strings["savedPath"]));
    }

    [Fact]
    public async Task AutomationPlan_ExtractSources_StepStoresUrls()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New()
            .Navigate(pageHost.ControlsUri.ToString())
            .FindElements("assetImagesRef", new(ElementLocator.CssSelector(".asset-image")))
            .ExtractSources("assetImagesRef", "assetUrls", ["src"])
            .Build();

        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.True(result.Snapshot.StringLists["assetUrls"].Count >= 2);
    }

    [Fact]
    public async Task AutomationPlan_DownloadUrlsToDirectory_StepDownloadsFiles()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        var tempDir = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", Guid.NewGuid().ToString("N"));
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New()
            .Navigate(pageHost.ControlsUri.ToString())
            .FindElements("assetImagesRef", new(ElementLocator.CssSelector(".asset-image")))
            .ExtractSources("assetImagesRef", "assetUrls", ["src"])
            .DownloadUrlsToDirectory("assetUrls", tempDir, "asset")
            .Build();

        await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.True(Directory.GetFiles(tempDir).Length >= 2);
    }

    [Fact]
    public async Task AutomationPlan_UpsertJsonRecords_StepCallsSink()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        var sink = new RecordingDataSink();
        await using var provider = CreatePlanRunnerProvider(s => s.AddSingleton<IAutomationPlanDataSink>(sink));
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New().StoreLiteral("payload", "echo=value").UpsertJsonRecords("payload", "step-target").Build();
        await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Single(sink.Records);
    }

    [Fact]
    public async Task AutomationPlan_UploadDirectoryToFileStorage_StepCallsStorage()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        var storage = new RecordingFileStorage();
        var uploadDirectory = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(uploadDirectory);
        await File.WriteAllTextAsync(Path.Combine(uploadDirectory, "upload.txt"), "upload-payload", ct);
        await using var provider = CreatePlanRunnerProvider(s => s.AddSingleton<IAutomationPlanFileStorage>(storage));
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New().UploadDirectoryToFileStorage(uploadDirectory, "uploads/test", "uploadedFiles").Build();
        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Single(storage.Uploads);
        Assert.Single(result.Snapshot.StringLists["uploadedFiles"]);
    }

    [Fact]
    public async Task AutomationPlan_Dropdown_CustomWidget_SetsEcho()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New("custom dropdown")
            .Navigate(pageHost.ControlsUri.ToString())
            .FindElement("trigger", ElementLocator.Id("customDropdownTrigger"))
            .ElementAction(
                "trigger",
                new DropdownElementAction(
                    OptionLocator: ElementLocator.CssSelector("#customDropdownList [data-value=\"b\"]"),
                    ClickTriggerFirst: true,
                    ScopeParentRef: null))
            .FindElement("echoEl", ElementLocator.Id("customDropdownEcho"))
            .ExtractElementData("echoEl", "picked", ElementDataExtractKind.Text)
            .Build();

        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Equal("picked:b", result.Snapshot.Strings["picked"].Trim());
    }

    [Fact]
    public async Task AutomationPlan_Dropdown_NativeSelect_ByValue_SetsEcho()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New("native dropdown")
            .Navigate(pageHost.ControlsUri.ToString())
            .FindElement("nativeSel", ElementLocator.Id("planNativeSel"))
            .ElementAction("nativeSel", new DropdownElementAction(SelectByValue: "b"))
            .FindElement("echoEl", ElementLocator.Id("planNativeEcho"))
            .ExtractElementData("echoEl", "nativeEcho", ElementDataExtractKind.Text)
            .Build();

        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Equal("native:b", result.Snapshot.Strings["nativeEcho"].Trim());
    }

    [Fact]
    public async Task AutomationPlan_FindDescendant_ClicksNestedOption()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await using var provider = CreatePlanRunnerProvider();
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New("find descendant")
            .Navigate(pageHost.ControlsUri.ToString())
            .FindElement("wrap", ElementLocator.Id("customDropdownWrap"))
            .FindElement("trigger", ElementLocator.Id("customDropdownTrigger"))
            .ElementAction("trigger", new ClickElementAction())
            .FindDescendant("wrap", "optA", ElementLocator.CssSelector("[data-value=\"a\"]"))
            .ElementAction("optA", new ClickElementAction())
            .FindElement("echoEl", ElementLocator.Id("customDropdownEcho"))
            .ExtractElementData("echoEl", "picked", ElementDataExtractKind.Text)
            .Build();

        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Equal("picked:a", result.Snapshot.Strings["picked"].Trim());
    }

    [Fact]
    public async Task AutomationPlan_InvokeDiMethod_StepStoresResult()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var session = await factory.CreateSessionAsync(ct);
        await using var provider = CreatePlanRunnerProvider(s => s.AddSingleton<PlanProbeService>());
        var runner = provider.GetRequiredService<IAutomationPlanRunner>();
        var plan = AutomationPlanBuilder.New()
            .StoreLiteral("value", "hello")
            .InvokeDiMethod(
                typeof(PlanProbeService).AssemblyQualifiedName!, nameof(PlanProbeService.BuildResult), new() { ["value"] = "{strings.value}" }, resultVariableName: "probeResult")
            .Build();

        var result = await runner.RunWithResultAsync(session, plan, null, null, ct);
        Assert.Equal("probe:hello", result.Snapshot.Strings["probeResult"]);
    }

    private static ServiceProvider CreatePlanRunnerProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddWebAutomationPlanRunner();
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static string GetAssetPath(string relativePath) => Path.Combine(AppContext.BaseDirectory, "TestAssets", relativePath);

    private sealed class RecordingDataSink : IAutomationPlanDataSink
    {
        public List<(string targetName, string jsonPayload)> Records { get; } = new();

        public Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct)
        {
            Records.Add((targetName, jsonPayload));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingFileStorage : IAutomationPlanFileStorage
    {
        public List<(string sourceDirectory, string destinationPrefix)> Uploads { get; } = new();

        public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
        {
            Uploads.Add((sourceDirectory, destinationPrefix));
            IReadOnlyList<string> uploaded = ["uploaded://sample/upload.txt"];
            return Task.FromResult(uploaded);
        }
    }

    private sealed class PlanProbeService
    {
        public Task<string> BuildResult(string value) => Task.FromResult($"probe:{value}");
    }

    private sealed class JsonPayloadService
    {
        public Task<string> ReadJson(string path) => Task.FromResult(File.ReadAllText(path));
    }
}