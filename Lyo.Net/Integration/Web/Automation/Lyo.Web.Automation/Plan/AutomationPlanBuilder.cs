using System.Collections.ObjectModel;
using Lyo.Exceptions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Plan;

/// <summary>Fluent construction of an <see cref="AutomationPlan" /> (same step types as JSON deserialization).</summary>
public sealed class AutomationPlanBuilder
{
    private readonly string? _name;
    private readonly List<AutomationStepDefinition> _steps = new();

    private AutomationPlanBuilder(string? name) => _name = name;

    /// <summary>Starts a named or unnamed plan.</summary>
    public static AutomationPlanBuilder New(string? name = null) => new(name);

    /// <summary>Builds the plan with an immutable step list and a time-ordered <see cref="AutomationStepDefinition.StepId" /> for any step that did not set one.</summary>
    public AutomationPlan Build()
    {
        var finalized = new List<AutomationStepDefinition>(_steps.Count);
        foreach (var step in _steps)
            finalized.Add(step.StepId is { } id && id != Guid.Empty ? step : step with { StepId = AutomationGuid.CreateTimeOrdered() });

        return new(_name, new ReadOnlyCollection<AutomationStepDefinition>(finalized));
    }

    /// <summary>Navigate; <paramref name="url" /> may include <c>{{variableName}}</c> placeholders filled from saved string variables.</summary>
    public AutomationPlanBuilder Navigate(string url, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);
        _steps.Add(new NavigateAutomationStep(url, stepName));
        return this;
    }

    public AutomationPlanBuilder Reload(string? stepName = null)
    {
        _steps.Add(new ReloadAutomationStep(stepName));
        return this;
    }

    /// <summary>Sets viewport/window size for the active tab (Playwright: layout viewport; Selenium: window size).</summary>
    public AutomationPlanBuilder SetViewportSize(int width, int height, string? stepName = null)
    {
        ArgumentHelpers.ThrowIf(width <= 0, "Width must be positive.", nameof(width));
        ArgumentHelpers.ThrowIf(height <= 0, "Height must be positive.", nameof(height));
        _steps.Add(new SetViewportSizeAutomationStep(width, height, stepName));
        return this;
    }

    /// <summary>Switches tab by zero-based index (<see cref="Lyo.Web.Automation.Abstractions.IWebAutomationTabs" />).</summary>
    public AutomationPlanBuilder SwitchToTabByIndex(int tabIndex, string? stepName = null)
    {
        ArgumentHelpers.ThrowIf(tabIndex < 0, "Tab index must be non-negative.", nameof(tabIndex));
        _steps.Add(new SwitchToTabByIndexAutomationStep(tabIndex, stepName));
        return this;
    }

    /// <summary>Switches tab by opaque key (template-expanded).</summary>
    public AutomationPlanBuilder SwitchToTabByKey(string tabKey, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tabKey);
        _steps.Add(new SwitchToTabByKeyAutomationStep(tabKey, stepName));
        return this;
    }

    /// <summary>Opens a new tab; <paramref name="url" /> may use template placeholders; optionally stores new tab key in a string variable.</summary>
    public AutomationPlanBuilder OpenNewTab(string? url = null, string? tabKeyVariableName = null, string? stepName = null)
    {
        _steps.Add(new OpenNewTabAutomationStep(url, tabKeyVariableName, stepName));
        return this;
    }

    /// <summary>Closes the current tab.</summary>
    public AutomationPlanBuilder CloseCurrentTab(string? stepName = null)
    {
        _steps.Add(new CloseCurrentTabAutomationStep(stepName));
        return this;
    }

    public AutomationPlanBuilder Delay(int delayMilliseconds, string? stepName = null)
    {
        _steps.Add(new DelayAutomationStep(delayMilliseconds, stepName));
        return this;
    }

    /// <summary>Finds a single element; <paramref name="chain" /> may be a one-segment path (implicit from <see cref="ElementLocator" />).</summary>
    public AutomationPlanBuilder FindElement(string refName, ElementLocatorChain chain, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
        ArgumentHelpers.ThrowIfNull(chain);
        if (chain.Segments.Count == 1)
            _steps.Add(new FindElementAutomationStep(refName, chain.Segments[0], stepName));
        else
            _steps.Add(new FindElementChainAutomationStep(refName, chain, stepName));

        return this;
    }

    /// <summary>Finds all elements matching the chained path and stores them under <paramref name="refName" />.</summary>
    public AutomationPlanBuilder FindElements(string refName, ElementLocatorChain chain, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
        ArgumentHelpers.ThrowIfNull(chain);
        _steps.Add(new FindElementsChainAutomationStep(refName, chain, stepName));
        return this;
    }

    public AutomationPlanBuilder ElementAction(string elementRefName, ElementAction action, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(elementRefName);
        ArgumentHelpers.ThrowIfNull(action);
        _steps.Add(new ElementActionAutomationStep(elementRefName, action, stepName));
        return this;
    }

    public AutomationPlanBuilder FindAndAct(string refName, ElementLocator locator, ElementAction action, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
        ArgumentHelpers.ThrowIfNull(locator);
        ArgumentHelpers.ThrowIfNull(action);
        _steps.Add(new FindAndActAutomationStep(refName, locator, action, stepName));
        return this;
    }

    public AutomationPlanBuilder FindAndActChain(string refName, ElementLocatorChain chain, ElementAction action, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
        ArgumentHelpers.ThrowIfNull(chain);
        ArgumentHelpers.ThrowIfNull(action);
        _steps.Add(new FindAndActChainAutomationStep(refName, chain, action, stepName));
        return this;
    }

    /// <summary>Stores attribute or visible text from one element ref into a string variable.</summary>
    public AutomationPlanBuilder ExtractElementData(string elementRefName, string variableName, ElementDataExtractKind kind, string? attributeName = null, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(elementRefName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        _steps.Add(new ExtractElementDataAutomationStep(elementRefName, variableName, kind, attributeName, stepName));
        return this;
    }

    /// <summary>Maps every element in a list ref to text or attribute values; stored as a string list variable.</summary>
    public AutomationPlanBuilder ExtractElementsListData(
        string elementsListRefName,
        string variableName,
        ElementDataExtractKind kind,
        string? attributeName = null,
        string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(elementsListRefName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        _steps.Add(new ExtractElementsListDataAutomationStep(elementsListRefName, variableName, kind, attributeName, stepName));
        return this;
    }

    /// <inheritdoc cref="ExtractElementData" />
    public AutomationPlanBuilder StoreFromElement(string elementRefName, string variableName, ElementDataExtractKind kind, string? attributeName = null, string? stepName = null)
        => ExtractElementData(elementRefName, variableName, kind, attributeName, stepName);

    /// <summary>Stores a literal string (may contain <c>{{var}}</c> expanded from existing string variables).</summary>
    public AutomationPlanBuilder StoreLiteral(string variableName, string value, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        ArgumentHelpers.ThrowIfNull(value);
        _steps.Add(new StoreLiteralAutomationStep(variableName, value, stepName));
        return this;
    }

    /// <summary>Builds a string from a template using <c>{{var}}</c> placeholders, then stores it.</summary>
    public AutomationPlanBuilder StoreTemplate(string variableName, string template, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        ArgumentHelpers.ThrowIfNull(template);
        _steps.Add(new StoreTemplateAutomationStep(variableName, template, stepName));
        return this;
    }

    /// <summary>Maps every item in a source string-list variable through a template and stores the resulting list.</summary>
    public AutomationPlanBuilder StoreStringListFromTemplate(string sourceVariableName, string variableName, string itemTemplate, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceVariableName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        ArgumentHelpers.ThrowIfNull(itemTemplate);
        _steps.Add(new StoreStringListFromTemplateAutomationStep(sourceVariableName, variableName, itemTemplate, stepName));
        return this;
    }

    /// <summary>Stores the current page URL in a string variable.</summary>
    public AutomationPlanBuilder StorePageUrl(string variableName, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        _steps.Add(new StorePageUrlAutomationStep(variableName, stepName));
        return this;
    }

    /// <summary>Stores the current document title in a string variable.</summary>
    public AutomationPlanBuilder StorePageTitle(string variableName, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        _steps.Add(new StorePageTitleAutomationStep(variableName, stepName));
        return this;
    }

    /// <summary>File path may include <c>{{var}}</c> placeholders.</summary>
    public AutomationPlanBuilder WriteStringListToFile(string variableName, string filePath, bool append = false, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        _steps.Add(new WriteStringListToFileAutomationStep(variableName, filePath, append, stepName));
        return this;
    }

    /// <summary>Downloads each URL in a string-list variable (requires runner HTTP dependency registration).</summary>
    /// <param name="urlListFromCompletedStepIndex">When set, use the list as it was after this zero-based step; when null, use the variable’s final value.</param>
    public AutomationPlanBuilder DownloadUrlsToDirectory(
        string urlListVariableName,
        string targetDirectory,
        string? fileNamePrefix = null,
        int? urlListFromCompletedStepIndex = null,
        string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(urlListVariableName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetDirectory);
        _steps.Add(new DownloadUrlsToDirectoryAutomationStep(urlListVariableName, targetDirectory, fileNamePrefix, stepName, urlListFromCompletedStepIndex));
        return this;
    }

    /// <summary>Sends an HTTP request with optional templated headers/body and stores response metadata in string variables.</summary>
    public AutomationPlanBuilder HttpRequest(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        string? bodyTemplate = null,
        string? responseBodyVariableName = null,
        string? statusCodeVariableName = null,
        string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(method);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);
        _steps.Add(new HttpRequestAutomationStep(method, url, headers, bodyTemplate, responseBodyVariableName, statusCodeVariableName, stepName));
        return this;
    }

    /// <summary>Downloads a single HTTP(S) URL to a specific file path.</summary>
    public AutomationPlanBuilder DownloadFile(string url, string targetFilePath, string? savedFilePathVariableName = null, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetFilePath);
        _steps.Add(new DownloadFileAutomationStep(url, targetFilePath, savedFilePathVariableName, stepName));
        return this;
    }

    /// <summary>Extracts source/link values from configured attributes on a previously-found element list.</summary>
    public AutomationPlanBuilder ExtractSources(
        string elementsListRefName,
        string variableName,
        IReadOnlyList<string>? attributeNames = null,
        bool resolveRelativeUrls = true,
        bool deduplicate = true,
        bool splitCommaSeparatedValues = true,
        string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(elementsListRefName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName);
        _steps.Add(new ExtractSourcesAutomationStep(elementsListRefName, variableName, attributeNames, resolveRelativeUrls, deduplicate, splitCommaSeparatedValues, stepName));
        return this;
    }

    /// <summary>Upserts a JSON payload string through the configured runtime data sink.</summary>
    public AutomationPlanBuilder UpsertJsonRecords(string recordsJsonVariableName, string targetName, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(recordsJsonVariableName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetName);
        _steps.Add(new UpsertJsonRecordsAutomationStep(recordsJsonVariableName, targetName, stepName));
        return this;
    }

    /// <summary>Uploads all files from a directory through the configured runtime file storage service.</summary>
    public AutomationPlanBuilder UploadDirectoryToFileStorage(string sourceDirectory, string destinationPrefix, string? storedFileListVariableName = null, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(destinationPrefix);
        _steps.Add(new UploadDirectoryToFileStorageAutomationStep(sourceDirectory, destinationPrefix, storedFileListVariableName, stepName));
        return this;
    }

    /// <summary>Invokes a DI-resolved service method for advanced scrape/intercept logic.</summary>
    public AutomationPlanBuilder InvokeDiMethod(
        string serviceType,
        string methodName,
        Dictionary<string, string>? arguments = null,
        bool throwOnMissingMethod = true,
        string? resultVariableName = null,
        string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(serviceType);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(methodName);
        _steps.Add(new InvokeDiMethodAutomationStep(serviceType, methodName, arguments, throwOnMissingMethod, resultVariableName, stepName));
        return this;
    }
}