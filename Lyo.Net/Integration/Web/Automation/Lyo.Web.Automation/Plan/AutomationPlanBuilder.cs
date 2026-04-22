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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url, nameof(url));
        _steps.Add(new NavigateAutomationStep(url, stepName));
        return this;
    }

    public AutomationPlanBuilder Reload(string? stepName = null)
    {
        _steps.Add(new ReloadAutomationStep(stepName));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName, nameof(refName));
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        if (chain.Segments.Count == 1)
            _steps.Add(new FindElementAutomationStep(refName, chain.Segments[0], stepName));
        else
            _steps.Add(new FindElementChainAutomationStep(refName, chain, stepName));

        return this;
    }

    /// <summary>Finds all elements matching the chained path and stores them under <paramref name="refName" />.</summary>
    public AutomationPlanBuilder FindElements(string refName, ElementLocatorChain chain, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName, nameof(refName));
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        _steps.Add(new FindElementsChainAutomationStep(refName, chain, stepName));
        return this;
    }

    public AutomationPlanBuilder ElementAction(string elementRefName, ElementAction action, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(elementRefName, nameof(elementRefName));
        ArgumentHelpers.ThrowIfNull(action, nameof(action));
        _steps.Add(new ElementActionAutomationStep(elementRefName, action, stepName));
        return this;
    }

    public AutomationPlanBuilder FindAndAct(string refName, ElementLocator locator, ElementAction action, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName, nameof(refName));
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        ArgumentHelpers.ThrowIfNull(action, nameof(action));
        _steps.Add(new FindAndActAutomationStep(refName, locator, action, stepName));
        return this;
    }

    public AutomationPlanBuilder FindAndActChain(string refName, ElementLocatorChain chain, ElementAction action, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName, nameof(refName));
        ArgumentHelpers.ThrowIfNull(chain, nameof(chain));
        ArgumentHelpers.ThrowIfNull(action, nameof(action));
        _steps.Add(new FindAndActChainAutomationStep(refName, chain, action, stepName));
        return this;
    }

    /// <summary>Stores attribute or visible text from one element ref into a string variable.</summary>
    public AutomationPlanBuilder ExtractElementData(string elementRefName, string variableName, ElementDataExtractKind kind, string? attributeName = null, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(elementRefName, nameof(elementRefName));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName, nameof(variableName));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(elementsListRefName, nameof(elementsListRefName));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName, nameof(variableName));
        _steps.Add(new ExtractElementsListDataAutomationStep(elementsListRefName, variableName, kind, attributeName, stepName));
        return this;
    }

    /// <inheritdoc cref="ExtractElementData" />
    public AutomationPlanBuilder StoreFromElement(string elementRefName, string variableName, ElementDataExtractKind kind, string? attributeName = null, string? stepName = null)
        => ExtractElementData(elementRefName, variableName, kind, attributeName, stepName);

    /// <summary>Stores a literal string (may contain <c>{{var}}</c> expanded from existing string variables).</summary>
    public AutomationPlanBuilder StoreLiteral(string variableName, string value, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName, nameof(variableName));
        ArgumentHelpers.ThrowIfNull(value, nameof(value));
        _steps.Add(new StoreLiteralAutomationStep(variableName, value, stepName));
        return this;
    }

    /// <summary>Builds a string from a template using <c>{{var}}</c> placeholders, then stores it.</summary>
    public AutomationPlanBuilder StoreTemplate(string variableName, string template, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName, nameof(variableName));
        ArgumentHelpers.ThrowIfNull(template, nameof(template));
        _steps.Add(new StoreTemplateAutomationStep(variableName, template, stepName));
        return this;
    }

    /// <summary>Stores the current page URL in a string variable.</summary>
    public AutomationPlanBuilder StorePageUrl(string variableName, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName, nameof(variableName));
        _steps.Add(new StorePageUrlAutomationStep(variableName, stepName));
        return this;
    }

    /// <summary>Stores the current document title in a string variable.</summary>
    public AutomationPlanBuilder StorePageTitle(string variableName, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName, nameof(variableName));
        _steps.Add(new StorePageTitleAutomationStep(variableName, stepName));
        return this;
    }

    /// <summary>File path may include <c>{{var}}</c> placeholders.</summary>
    public AutomationPlanBuilder WriteStringListToFile(string variableName, string filePath, bool append = false, string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableName, nameof(variableName));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        _steps.Add(new WriteStringListToFileAutomationStep(variableName, filePath, append, stepName));
        return this;
    }

    /// <summary>Downloads each URL in a string-list variable (requires <see cref="AutomationPlanRuntimeOptions.HttpClient" /> at run time).</summary>
    /// <param name="urlListFromCompletedStepIndex">When set, use the list as it was after this zero-based step; when null, use the variable’s final value.</param>
    public AutomationPlanBuilder DownloadUrlsToDirectory(
        string urlListVariableName,
        string targetDirectory,
        string? fileNamePrefix = null,
        int? urlListFromCompletedStepIndex = null,
        string? stepName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(urlListVariableName, nameof(urlListVariableName));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetDirectory, nameof(targetDirectory));
        _steps.Add(new DownloadUrlsToDirectoryAutomationStep(urlListVariableName, targetDirectory, fileNamePrefix, stepName, urlListFromCompletedStepIndex));
        return this;
    }
}