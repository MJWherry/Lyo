using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Models;

/// <summary>One step in an <see cref="AutomationPlan" /> (consumers may serialize with their own JSON; typical shape uses polymorphic <c>type</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NavigateAutomationStep), "navigate")]
[JsonDerivedType(typeof(FindElementAutomationStep), "findElement")]
[JsonDerivedType(typeof(ElementActionAutomationStep), "elementAction")]
[JsonDerivedType(typeof(FindAndActAutomationStep), "findAndAct")]
[JsonDerivedType(typeof(FindElementChainAutomationStep), "findElementChain")]
[JsonDerivedType(typeof(FindAndActChainAutomationStep), "findAndActChain")]
[JsonDerivedType(typeof(DelayAutomationStep), "delay")]
[JsonDerivedType(typeof(ReloadAutomationStep), "reload")]
[JsonDerivedType(typeof(SetViewportSizeAutomationStep), "setViewportSize")]
[JsonDerivedType(typeof(SwitchToTabByIndexAutomationStep), "switchTabByIndex")]
[JsonDerivedType(typeof(SwitchToTabByKeyAutomationStep), "switchTabByKey")]
[JsonDerivedType(typeof(OpenNewTabAutomationStep), "openNewTab")]
[JsonDerivedType(typeof(CloseCurrentTabAutomationStep), "closeCurrentTab")]
[JsonDerivedType(typeof(FindElementsChainAutomationStep), "findElementsChain")]
[JsonDerivedType(typeof(ExtractElementDataAutomationStep), "extractElementData")]
[JsonDerivedType(typeof(ExtractElementsListDataAutomationStep), "extractElementsListData")]
[JsonDerivedType(typeof(WriteStringListToFileAutomationStep), "writeStringListToFile")]
[JsonDerivedType(typeof(DownloadUrlsToDirectoryAutomationStep), "downloadUrlsToDirectory")]
[JsonDerivedType(typeof(StoreLiteralAutomationStep), "storeLiteral")]
[JsonDerivedType(typeof(StoreTemplateAutomationStep), "storeTemplate")]
[JsonDerivedType(typeof(StoreStringListFromTemplateAutomationStep), "storeStringListFromTemplate")]
[JsonDerivedType(typeof(StorePageUrlAutomationStep), "storePageUrl")]
[JsonDerivedType(typeof(StorePageTitleAutomationStep), "storePageTitle")]
[JsonDerivedType(typeof(HttpRequestAutomationStep), "httpRequest")]
[JsonDerivedType(typeof(DownloadFileAutomationStep), "downloadFile")]
[JsonDerivedType(typeof(ExtractSourcesAutomationStep), "extractSources")]
[JsonDerivedType(typeof(UpsertJsonRecordsAutomationStep), "upsertJsonRecords")]
[JsonDerivedType(typeof(UploadDirectoryToFileStorageAutomationStep), "uploadDirectoryToFileStorage")]
[JsonDerivedType(typeof(InvokeDiMethodAutomationStep), "invokeDiMethod")]
public abstract record AutomationStepDefinition(string? Name = null)
{
    /// <summary>Stable id for this step (set by <see cref="Lyo.Web.Automation.Plan.AutomationPlanBuilder.Build" /> when omitted).</summary>
    public Guid? StepId { get; init; }

    /// <summary>Maximum duration for this step; overrides <see cref="Lyo.Web.Automation.Plan.AutomationPlanRuntimeOptions.DefaultStepTimeout" /> when set.</summary>
    public TimeSpan? StepTimeout { get; init; }

    /// <summary>Suffix for <see cref="ToString" />: optional step id, timeout, and display name.</summary>
    protected string FormatStepDebugLine(string head)
    {
        var sb = new StringBuilder(head);
        if (StepId is { } sid && sid != Guid.Empty)
            sb.Append($" | stepId={sid:N}");

        if (StepTimeout is { } t)
            sb.Append($" | timeout={t}");

        sb.Append(AutomationDisplayText.OptionalName(Name));
        return sb.ToString();
    }
}

/// <summary>Navigates the active page to <see cref="Url" /> (supports <c>{{var}}</c> from string variables).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record NavigateAutomationStep(string Url, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"navigate → {AutomationDisplayText.Ellipsis(Url)}");
}

/// <summary>Polls for an element, then stores it as <see cref="RefName" /> for later <see cref="ElementActionAutomationStep" /> steps.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FindElementAutomationStep(string RefName, ElementLocator Locator, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"findElement ref={RefName} @ {Locator}");
}

/// <summary>Runs an <see cref="ElementAction" /> on a previously stored element ref.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ElementActionAutomationStep(string ElementRefName, ElementAction Action, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"elementAction ref={ElementRefName} {Action}");
}

/// <summary>Convenience: find by locator, store as <see cref="RefName" />, then apply <see cref="Action" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FindAndActAutomationStep(string RefName, ElementLocator Locator, ElementAction Action, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"findAndAct ref={RefName} @ {Locator} → {Action}");
}

/// <summary>Polls for a chained locator path, then stores the result as <see cref="RefName" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FindElementChainAutomationStep(string RefName, ElementLocatorChain Chain, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"findElementChain ref={RefName} chain={Chain}");
}

/// <summary>Finds via <see cref="Chain" />, stores as <see cref="RefName" />, then applies <see cref="Action" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FindAndActChainAutomationStep(string RefName, ElementLocatorChain Chain, ElementAction Action, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"findAndActChain ref={RefName} chain={Chain} → {Action}");
}

/// <summary>Async delay between steps (for pacing or waiting on client-side rendering).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DelayAutomationStep(int DelayMilliseconds, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"delay {DelayMilliseconds} ms");
}

/// <summary>Reloads the current document (F5).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ReloadAutomationStep(string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine("reload");
}

/// <summary>Sets viewport/window dimensions for the active tab (engine-specific semantics; see <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationPage.SetViewportSizeAsync" />).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SetViewportSizeAutomationStep(int Width, int Height, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"setViewportSize {Width}x{Height}");
}

/// <summary>Switches active tab/page by zero-based index (<see cref="Lyo.Web.Automation.Abstractions.IWebAutomationTabs.SwitchToTabAsync(int, CancellationToken)" />).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SwitchToTabByIndexAutomationStep(int TabIndex, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"switchTabByIndex index={TabIndex}");
}

/// <summary>Switches active tab/page by opaque tab key (template-expanded). Selenium: window handle; Playwright: page key hex.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SwitchToTabByKeyAutomationStep(string TabKey, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"switchTabByKey key={AutomationDisplayText.Ellipsis(TabKey, 48)}");
}

/// <summary>Opens a new tab; optional URL template; optional variable to store the new tab key.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record OpenNewTabAutomationStep(string? Url = null, string? TabKeyVariableName = null, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var store = string.IsNullOrWhiteSpace(TabKeyVariableName) ? "" : $" storeKey→{TabKeyVariableName}";
        return FormatStepDebugLine($"openNewTab url={AutomationDisplayText.Ellipsis(Url ?? "", 48)}{store}");
    }
}

/// <summary>Closes the current tab/page and activates another if any remain.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CloseCurrentTabAutomationStep(string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine("closeCurrentTab");
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record FindElementsChainAutomationStep(string RefName, ElementLocatorChain Chain, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"findElementsChain ref={RefName} chain={Chain}");
}

/// <summary>Reads attribute or text from a stored element ref into a string variable (usable later as <c>{{variableName}}</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ExtractElementDataAutomationStep(string ElementRefName, string VariableName, ElementDataExtractKind Kind, string? AttributeName = null, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var attr = Kind == ElementDataExtractKind.Attribute ? $" attr={AutomationDisplayText.Ellipsis(AttributeName ?? "", 32)}" : "";
        return FormatStepDebugLine($"extractElementData from={ElementRefName} → var:{VariableName} ({Kind}{attr})");
    }
}

/// <summary>Maps every element in a list ref to attribute or text; results stored as a string list variable.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ExtractElementsListDataAutomationStep(
    string ElementsListRefName,
    string VariableName,
    ElementDataExtractKind Kind,
    string? AttributeName = null,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var attr = Kind == ElementDataExtractKind.Attribute ? $" attr={AutomationDisplayText.Ellipsis(AttributeName ?? "", 32)}" : "";
        return FormatStepDebugLine($"extractElementsListData from={ElementsListRefName} → var:{VariableName} ({Kind}{attr})");
    }
}

/// <summary>Writes all lines in a string-list variable to a UTF-8 file (creates parent directories).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record WriteStringListToFileAutomationStep(string VariableName, string FilePath, bool Append = false, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"writeStringListToFile var:{VariableName} → {AutomationDisplayText.Ellipsis(FilePath, 80)} (append: {Append})");
}

/// <summary>
/// Downloads each URL in a string-list variable into <see cref="TargetDirectory" /> (requires runner HTTP dependency registration). When
/// <see cref="UrlListFromCompletedStepIndex" /> is set, URLs are read from that step’s snapshot (zero-based completed step index); when null, the current (final) variable
/// value is used.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DownloadUrlsToDirectoryAutomationStep(
    string UrlListVariableName,
    string TargetDirectory,
    string? FileNamePrefix = null,
    string? Name = null,
    int? UrlListFromCompletedStepIndex = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var stepIdx = UrlListFromCompletedStepIndex is { } i ? $" fromStepIndex={i}" : "";
        return FormatStepDebugLine(
            $"downloadUrls var:{UrlListVariableName} → {AutomationDisplayText.Ellipsis(TargetDirectory, 64)} prefix={FileNamePrefix ?? "(default)"}{stepIdx}");
    }
}

/// <summary>Stores a string literal in <see cref="VariableName" /> (value may contain <c>{{otherVar}}</c> placeholders).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoreLiteralAutomationStep(string VariableName, string Value, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"storeLiteral var:{VariableName} = {AutomationDisplayText.Ellipsis(Value, 64)}");
}

/// <summary>Expands <see cref="Template" /> using current string variables (<c>{{name}}</c>) and stores the result in <see cref="VariableName" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoreTemplateAutomationStep(string VariableName, string Template, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"storeTemplate var:{VariableName} ← {AutomationDisplayText.Ellipsis(Template, 64)}");
}

/// <summary>Maps each item in a source string-list variable through a template and stores the result list.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoreStringListFromTemplateAutomationStep(
    string SourceVariableName,
    string VariableName,
    string ItemTemplate,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
        => FormatStepDebugLine(
            $"storeStringListFromTemplate src:{SourceVariableName} -> var:{VariableName} template={AutomationDisplayText.Ellipsis(ItemTemplate, 64)}");
}

/// <summary>Stores the current document URL in <see cref="VariableName" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StorePageUrlAutomationStep(string VariableName, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"storePageUrl var:{VariableName}");
}

/// <summary>Stores the current document title in <see cref="VariableName" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StorePageTitleAutomationStep(string VariableName, string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString() => FormatStepDebugLine($"storePageTitle var:{VariableName}");
}

/// <summary>Sends an HTTP request using runtime <c>HttpClient</c>, with optional templated headers and body.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record HttpRequestAutomationStep(
    string Method,
    string Url,
    Dictionary<string, string>? Headers = null,
    string? BodyTemplate = null,
    string? ResponseBodyVariableName = null,
    string? StatusCodeVariableName = null,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
        => FormatStepDebugLine($"httpRequest {Method} {AutomationDisplayText.Ellipsis(Url, 96)}");
}

/// <summary>Downloads a single HTTP(S) file URL to disk using runtime <c>HttpClient</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DownloadFileAutomationStep(
    string Url,
    string TargetFilePath,
    string? SavedFilePathVariableName = null,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
        => FormatStepDebugLine($"downloadFile {AutomationDisplayText.Ellipsis(Url, 64)} -> {AutomationDisplayText.Ellipsis(TargetFilePath, 64)}");
}

/// <summary>Extracts source/link values from attributes on an element list into a string-list variable.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ExtractSourcesAutomationStep(
    string ElementsListRefName,
    string VariableName,
    IReadOnlyList<string>? AttributeNames = null,
    bool ResolveRelativeUrls = true,
    bool Deduplicate = true,
    bool SplitCommaSeparatedValues = true,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
        => FormatStepDebugLine(
            $"extractSources from={ElementsListRefName} -> var:{VariableName} attrs={(AttributeNames == null ? "(default)" : string.Join(",", AttributeNames))} resolveRelative={ResolveRelativeUrls} dedupe={Deduplicate} splitCsv={SplitCommaSeparatedValues}");
}

/// <summary>Upserts JSON records through the configured runtime data sink abstraction.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record UpsertJsonRecordsAutomationStep(
    string RecordsJsonVariableName,
    string TargetName,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
        => FormatStepDebugLine($"upsertJsonRecords var:{RecordsJsonVariableName} -> target:{TargetName}");
}

/// <summary>Uploads all files from a local directory through the configured runtime file storage abstraction.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record UploadDirectoryToFileStorageAutomationStep(
    string SourceDirectory,
    string DestinationPrefix,
    string? StoredFileListVariableName = null,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
        => FormatStepDebugLine(
            $"uploadDirectoryToFileStorage {AutomationDisplayText.Ellipsis(SourceDirectory, 48)} -> {AutomationDisplayText.Ellipsis(DestinationPrefix, 48)}");
}

/// <summary>Invokes a DI-resolved service method for advanced scrape logic that may mutate shared run context.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record InvokeDiMethodAutomationStep(
    string ServiceType,
    string MethodName,
    Dictionary<string, string>? Arguments = null,
    bool ThrowOnMissingMethod = true,
    string? ResultVariableName = null,
    string? Name = null)
    : AutomationStepDefinition(Name)
{
    /// <inheritdoc />
    public override string ToString()
        => FormatStepDebugLine($"invokeDiMethod service={AutomationDisplayText.Ellipsis(ServiceType, 64)} method={MethodName}");
}