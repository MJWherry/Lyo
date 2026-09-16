using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Json;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Web.Components;

/// <summary>
/// Edits an <see cref="AutomationPlan" /> in memory. Steps the form does not know, such as <c>httpRequest</c>, stay in the document when the JSON is parsed and written back.
/// </summary>
public static class AutomationPlanDraft
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();

    /// <summary>Appends a navigate step and returns a new plan. The previous step list is left unchanged.</summary>
    public static AutomationPlan InsertNavigate(AutomationPlan plan, string url)
    {
        ArgumentHelpers.ThrowIfNull(plan);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);
        var steps = plan.Steps.ToList();
        steps.Add(new NavigateAutomationStep(url));
        return plan with { Steps = steps };
    }

    /// <summary>Moves the step at <paramref name="fromIndex" /> so it lands at <paramref name="toIndex" />.</summary>
    public static AutomationPlan MoveStep(AutomationPlan plan, int fromIndex, int toIndex)
    {
        ArgumentHelpers.ThrowIfNull(plan);
        var steps = plan.Steps.ToList();
        ArgumentHelpers.ThrowIf(fromIndex < 0 || fromIndex >= steps.Count, "fromIndex is outside the step list.", nameof(fromIndex));
        ArgumentHelpers.ThrowIf(toIndex < 0 || toIndex >= steps.Count, "toIndex is outside the step list.", nameof(toIndex));
        var step = steps[fromIndex];
        steps.RemoveAt(fromIndex);
        steps.Insert(toIndex, step);
        return plan with { Steps = steps };
    }

    /// <summary>Parses plan JSON and writes it back. A step type the form does not edit, such as <c>httpRequest</c>, is kept.</summary>
    public static string RoundTrip(string json)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        var plan = JsonSerializer.Deserialize<AutomationPlan>(json, Json) ?? throw new InvalidOperationException("Invalid plan JSON.");
        return JsonSerializer.Serialize(plan, Json);
    }

    /// <summary>A named plan with no steps.</summary>
    public static AutomationPlan CreateEmpty(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return new(name, []);
    }

    /// <summary>Readable name for a step <paramref name="type" />. Unknown types stay as the raw discriminator.</summary>
    public static string StepLabel(string type)
        => type switch {
            "navigate" => "Navigate",
            "findElement" => "Find element",
            "findDescendant" => "Find descendant",
            "elementAction" => "Element action",
            "findAndAct" => "Find and act",
            "findElementChain" => "Find element chain",
            "findAndActChain" => "Find and act chain",
            "delay" => "Delay",
            "captureNetwork" => "Capture network",
            "reload" => "Reload",
            "setViewportSize" => "Set viewport",
            "randomizeViewport" => "Randomize viewport",
            "switchTabByIndex" => "Switch tab by index",
            "switchTabByKey" => "Switch tab by key",
            "openNewTab" => "Open new tab",
            "closeCurrentTab" => "Close current tab",
            "findElementsChain" => "Find elements chain",
            "extractElementData" => "Extract element",
            "extractElementsListData" => "Extract element list",
            "writeStringListToFile" => "Write string list",
            "downloadUrlsToDirectory" => "Download URLs",
            "storeLiteral" => "Store literal",
            "storeStringList" => "Store string list",
            "storeTemplate" => "Store template",
            "storeStringListFromTemplate" => "Store list from template",
            "storePageUrl" => "Page URL",
            "storePageTitle" => "Page title",
            "httpRequest" => "HTTP request",
            "downloadFile" => "Download file",
            "extractSources" => "Extract sources",
            "upsertJsonRecords" => "Upsert JSON records",
            "uploadDirectoryToFileStorage" => "Upload directory",
            "invokeDiMethod" => "Invoke method",
            _ => type
        };

    /// <summary>JSON <c>type</c> values the workbench dropdown can insert. This is every runner step.</summary>
    public static IReadOnlyList<string> FormStepTypes { get; } = [
        "navigate",
        "findElement",
        "findDescendant",
        "elementAction",
        "findAndAct",
        "findElementChain",
        "findAndActChain",
        "delay",
        "captureNetwork",
        "reload",
        "setViewportSize",
        "randomizeViewport",
        "switchTabByIndex",
        "switchTabByKey",
        "openNewTab",
        "closeCurrentTab",
        "findElementsChain",
        "extractElementData",
        "extractElementsListData",
        "writeStringListToFile",
        "downloadUrlsToDirectory",
        "storeLiteral",
        "storeStringList",
        "storeTemplate",
        "storeStringListFromTemplate",
        "storePageUrl",
        "storePageTitle",
        "httpRequest",
        "downloadFile",
        "extractSources",
        "upsertJsonRecords",
        "uploadDirectoryToFileStorage",
        "invokeDiMethod"
    ];

    /// <summary>JSON options used by <see cref="ToJson" /> and <see cref="Parse" />.</summary>
    public static JsonSerializerOptions JsonOptions => Json;

    /// <summary>True when the workbench has fields for <paramref name="step" />. Other steps stay in the plan and are edited as JSON.</summary>
    public static bool HasForm(AutomationStepDefinition step)
        => FormStepTypes.Contains(StepType(step));

    /// <summary>JSON <c>type</c> discriminator for a step the form can insert.</summary>
    public static string StepType(AutomationStepDefinition step)
        => step switch {
            NavigateAutomationStep => "navigate",
            FindElementAutomationStep => "findElement",
            FindDescendantAutomationStep => "findDescendant",
            ElementActionAutomationStep => "elementAction",
            FindAndActAutomationStep => "findAndAct",
            FindElementChainAutomationStep => "findElementChain",
            FindAndActChainAutomationStep => "findAndActChain",
            DelayAutomationStep => "delay",
            CaptureNetworkAutomationStep => "captureNetwork",
            ReloadAutomationStep => "reload",
            SetViewportSizeAutomationStep => "setViewportSize",
            RandomizeViewportAutomationStep => "randomizeViewport",
            SwitchToTabByIndexAutomationStep => "switchTabByIndex",
            SwitchToTabByKeyAutomationStep => "switchTabByKey",
            OpenNewTabAutomationStep => "openNewTab",
            CloseCurrentTabAutomationStep => "closeCurrentTab",
            FindElementsChainAutomationStep => "findElementsChain",
            ExtractElementDataAutomationStep => "extractElementData",
            ExtractElementsListDataAutomationStep => "extractElementsListData",
            WriteStringListToFileAutomationStep => "writeStringListToFile",
            DownloadUrlsToDirectoryAutomationStep => "downloadUrlsToDirectory",
            StoreLiteralAutomationStep => "storeLiteral",
            StoreStringListAutomationStep => "storeStringList",
            StoreTemplateAutomationStep => "storeTemplate",
            StoreStringListFromTemplateAutomationStep => "storeStringListFromTemplate",
            StorePageUrlAutomationStep => "storePageUrl",
            StorePageTitleAutomationStep => "storePageTitle",
            HttpRequestAutomationStep => "httpRequest",
            DownloadFileAutomationStep => "downloadFile",
            ExtractSourcesAutomationStep => "extractSources",
            UpsertJsonRecordsAutomationStep => "upsertJsonRecords",
            UploadDirectoryToFileStorageAutomationStep => "uploadDirectoryToFileStorage",
            InvokeDiMethodAutomationStep => "invokeDiMethod",
            _ => step.GetType().Name
        };

    /// <summary>A new step of <paramref name="type" /> with empty fields the form can fill in.</summary>
    public static AutomationStepDefinition CreateStep(string type)
        => type switch {
            "navigate" => new NavigateAutomationStep("https://example.com/"),
            "reload" => new ReloadAutomationStep(),
            "delay" => new DelayAutomationStep(500),
            "findElement" => new FindElementAutomationStep("element", new ElementLocator(ElementLocatorKind.CssSelector, "#main")),
            "findAndAct" => new FindAndActAutomationStep("element", new ElementLocator(ElementLocatorKind.CssSelector, "button"), new ClickElementAction()),
            "extractElementData" => new ExtractElementDataAutomationStep("element", "value", ElementDataExtractKind.Text),
            "storeLiteral" => new StoreLiteralAutomationStep("value", ""),
            "storeTemplate" => new StoreTemplateAutomationStep("summary", "{strings.value}"),
            "storePageUrl" => new StorePageUrlAutomationStep("pageUrl"),
            "storePageTitle" => new StorePageTitleAutomationStep("pageTitle"),
            "captureNetwork" => new CaptureNetworkAutomationStep("urls", "", 3000),
            "storeStringList" => new StoreStringListAutomationStep("urls", []),
            "extractSources" => new ExtractSourcesAutomationStep("elements", "images", ["src", "data-src", "data-lazy-src", "srcset"]),
            "downloadUrlsToDirectory" => new DownloadUrlsToDirectoryAutomationStep("urls", "downloads"),
            "findDescendant" => new FindDescendantAutomationStep("parent", "child", new ElementLocator(ElementLocatorKind.CssSelector, ".item")),
            "elementAction" => new ElementActionAutomationStep("element", new ClickElementAction()),
            "findElementChain" => new FindElementChainAutomationStep("element", new ElementLocatorChain(new ElementLocator(ElementLocatorKind.CssSelector, "#main"))),
            "findAndActChain" => new FindAndActChainAutomationStep("element", new ElementLocatorChain(new ElementLocator(ElementLocatorKind.CssSelector, "button")), new ClickElementAction()),
            "findElementsChain" => new FindElementsChainAutomationStep("elements", new ElementLocatorChain(new ElementLocator(ElementLocatorKind.CssSelector, "a"))),
            "setViewportSize" => new SetViewportSizeAutomationStep(1280, 720),
            "randomizeViewport" => new RandomizeViewportAutomationStep([new DesktopDisplaySize(1280, 720)], 0),
            "switchTabByIndex" => new SwitchToTabByIndexAutomationStep(0),
            "switchTabByKey" => new SwitchToTabByKeyAutomationStep(""),
            "openNewTab" => new OpenNewTabAutomationStep("https://example.com/", "tabKey"),
            "closeCurrentTab" => new CloseCurrentTabAutomationStep(),
            "extractElementsListData" => new ExtractElementsListDataAutomationStep("elements", "values", ElementDataExtractKind.Text),
            "writeStringListToFile" => new WriteStringListToFileAutomationStep("urls", "urls.txt"),
            "storeStringListFromTemplate" => new StoreStringListFromTemplateAutomationStep("urls", "lines", "{item}"),
            "httpRequest" => new HttpRequestAutomationStep("GET", "https://example.com/"),
            "downloadFile" => new DownloadFileAutomationStep("https://example.com/file", "file.bin"),
            "upsertJsonRecords" => new UpsertJsonRecordsAutomationStep("records", "target"),
            "uploadDirectoryToFileStorage" => new UploadDirectoryToFileStorageAutomationStep("downloads", "automation/uploads"),
            "invokeDiMethod" => new InvokeDiMethodAutomationStep("Service", "Run"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "This step type is edited as JSON.")
        };

    /// <summary>Serializes <paramref name="plan" /> with the same JSON options the run API uses.</summary>
    public static string ToJson(AutomationPlan plan)
    {
        ArgumentHelpers.ThrowIfNull(plan);
        return JsonSerializer.Serialize(plan, Json);
    }

    /// <summary>Parses plan JSON written by <see cref="ToJson" /> or a recorded plan file.</summary>
    public static AutomationPlan Parse(string json)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<AutomationPlan>(json, Json) ?? throw new InvalidOperationException("Invalid plan JSON.");
    }
}
