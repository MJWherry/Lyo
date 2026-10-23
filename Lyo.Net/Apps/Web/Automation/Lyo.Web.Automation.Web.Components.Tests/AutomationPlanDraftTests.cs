using System.Text.Json;
using Lyo.Json;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Web.Components.Tests;

public sealed class AutomationPlanDraftTests
{
    [Fact]
    public void InsertStep_AppendsNavigate()
    {
        var plan = AutomationPlanDraft.InsertNavigate(new("demo", []), "https://example.com/start");
        var step = Assert.IsType<NavigateAutomationStep>(Assert.Single(plan.Steps));
        Assert.Equal("https://example.com/start", step.Url);
        Assert.Equal("demo", plan.Name);
    }

    [Fact]
    public void MoveStep_Reorders()
    {
        var plan = new AutomationPlan(null, [new NavigateAutomationStep("https://a.example"), new ReloadAutomationStep()]);
        var moved = AutomationPlanDraft.MoveStep(plan, 1, 0);
        Assert.IsType<ReloadAutomationStep>(moved.Steps[0]);
        Assert.IsType<NavigateAutomationStep>(moved.Steps[1]);
    }

    [Fact]
    public void RoundTrip_PreservesUnknownStepType()
    {
        const string json = """
                            {
                              "name": "keep",
                              "steps": [
                                { "type": "httpRequest", "method": "GET", "url": "https://example.com/hook" }
                              ]
                            }
                            """;

        var round = AutomationPlanDraft.RoundTrip(json);
        var plan = JsonSerializer.Deserialize<AutomationPlan>(round, LyoJsonSerializerOptions.Create());
        var step = Assert.IsType<HttpRequestAutomationStep>(Assert.Single(plan!.Steps));
        Assert.Equal("GET", step.Method);
        Assert.Equal("https://example.com/hook", step.Url);
        Assert.True(AutomationPlanDraft.HasForm(step));
    }

    [Fact]
    public void Catalog_CoversEveryRunnerStep()
    {
        string[] types = [
            "navigate", "findElement", "findDescendant", "elementAction", "findAndAct", "findElementChain", "findAndActChain", "delay", "captureNetwork", "reload",
            "setViewportSize", "randomizeViewport", "switchTabByIndex", "switchTabByKey", "openNewTab", "closeCurrentTab", "findElementsChain", "extractElementData",
            "extractElementsListData", "writeStringListToFile", "downloadUrlsToDirectory", "storeLiteral", "storeStringList", "storeTemplate", "storeStringListFromTemplate",
            "storePageUrl", "storePageTitle", "httpRequest", "downloadFile", "extractSources", "upsertJsonRecords", "uploadDirectoryToFileStorage", "invokeDiMethod"
        ];

        Assert.Equal(types, AutomationPlanDraft.FormStepTypes);
        foreach (var type in types) {
            var created = AutomationPlanDraft.CreateStep(type);
            Assert.Equal(type, AutomationPlanDraft.StepType(created));
            Assert.True(AutomationPlanDraft.HasForm(created));
            var json = AutomationPlanDraft.ToJson(new("catalog", [created]));
            var parsed = AutomationPlanDraft.Parse(json);
            Assert.Equal(type, AutomationPlanDraft.StepType(Assert.Single(parsed.Steps)));
        }

        var reload = Assert.IsType<ReloadAutomationStep>(AutomationPlanDraft.CreateStep("reload"));
        Assert.Null(reload.Name);
    }

    [Fact]
    public void CreateEmpty_HasTheNameAndNoSteps()
    {
        var plan = AutomationPlanDraft.CreateEmpty("Checkout");
        Assert.Equal("Checkout", plan.Name);
        Assert.Empty(plan.Steps);
    }

    [Fact]
    public void FormCatalog_IncludesCaptureDownloadStringListAndSources()
    {
        Assert.True(AutomationPlanDraft.HasForm(AutomationPlanDraft.CreateStep("captureNetwork")));
        Assert.True(AutomationPlanDraft.HasForm(AutomationPlanDraft.CreateStep("downloadUrlsToDirectory")));
        Assert.True(AutomationPlanDraft.HasForm(AutomationPlanDraft.CreateStep("storeStringList")));
        Assert.True(AutomationPlanDraft.HasForm(AutomationPlanDraft.CreateStep("extractSources")));
        Assert.Equal("Capture network", AutomationPlanDraft.StepLabel("captureNetwork"));
        Assert.Equal("Download URLs", AutomationPlanDraft.StepLabel("downloadUrlsToDirectory"));
        Assert.Equal("Store string list", AutomationPlanDraft.StepLabel("storeStringList"));
        Assert.Equal("Extract sources", AutomationPlanDraft.StepLabel("extractSources"));
    }
}