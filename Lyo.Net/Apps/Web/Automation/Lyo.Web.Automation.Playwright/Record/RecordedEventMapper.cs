using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>
/// Folds recorded browser events into an <see cref="AutomationPlan" />. Clicks that only focus a field are dropped once typing arrives. Typing on one field stays a single
/// step until another element, a key such as Enter, navigation, or <see cref="Build" />. A blur does not cut a new step. A navigation flagged as caused by the preceding
/// action does not become its own step.
/// </summary>
public sealed class RecordedEventMapper
{
    private static readonly string[] ImageAttributes = ["src", "data-src", "data-lazy-src", "srcset"];
    private readonly List<AutomationStepDefinition> _steps = [];
    private PendingClick? _click;
    private PendingInput? _input;
    private int _refs;
    private string? _name;

    /// <summary>Starts an empty plan with an optional display name.</summary>
    public RecordedEventMapper(string? planName = null) => _name = planName;

    /// <summary>Plan name written into <see cref="Build" />.</summary>
    public string? PlanName => _name;

    /// <summary>Drops the current steps and starts again. A URL that is not blank and not <c>about:</c> becomes the first navigate step.</summary>
    public void Reset(string? url)
    {
        _steps.Clear();
        _click = null;
        _input = null;
        _refs = 0;
        if (url is string start && !string.IsNullOrWhiteSpace(start) && !start.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            _steps.Add(new NavigateAutomationStep(start));
    }

    /// <summary>
    /// Applies one event. Pending typing on a field stays one step across further input and blur. It commits on another element, a key such as Enter, navigation, or
    /// <see cref="Build" />.
    /// </summary>
    public void Apply(RecordedDomEvent evt)
    {
        ArgumentHelpers.ThrowIfNull(evt);
        switch (evt.Kind) {
            case RecordedDomEventKind.Click:
                OnClick(evt);
                break;
            case RecordedDomEventKind.Input:
                OnInput(evt);
                break;
            case RecordedDomEventKind.Blur:
                OnBlur(evt);
                break;
            case RecordedDomEventKind.Select:
                OnSelect(evt);
                break;
            case RecordedDomEventKind.Key:
                OnKey(evt);
                break;
            case RecordedDomEventKind.Navigate:
                OnNavigate(evt);
                break;
            case RecordedDomEventKind.Pick:
                OnPick(evt);
                break;
            case RecordedDomEventKind.Insert:
                OnInsert(evt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(evt), evt.Kind, null);
        }
    }

    /// <summary>Commits pending typing or a held click, assigns missing step ids, and returns the plan.</summary>
    public AutomationPlan Build()
    {
        FlushPending();
        var finalized = new List<AutomationStepDefinition>(_steps.Count);
        foreach (var step in _steps)
            finalized.Add(step.StepId is { } id && id != Guid.Empty ? step : step with { StepId = AutomationGuid.CreateTimeOrdered() });

        _steps.Clear();
        _steps.AddRange(finalized);
        return new(_name, finalized);
    }

    /// <summary>Steps currently committed, plus any held click or input, as the panel shows them.</summary>
    public AutomationPlan Preview()
    {
        var next = _refs;
        var steps = new List<AutomationStepDefinition>(_steps);
        if (_input != null)
            steps.Add(new FindAndActAutomationStep($"el{++next}", _input.Locator, new InputTextElementAction(_input.Text, true)));

        if (_click != null)
            steps.Add(new FindAndActAutomationStep($"el{++next}", _click.Locator, new ClickElementAction()));

        return new(_name, steps);
    }

    /// <summary>
    /// Replaces the step list when <paramref name="json" /> parses. Invalid JSON leaves the current steps in place and returns the parser error.
    /// </summary>
    public RecordedPlanEdit ApplyJson(string json)
    {
        ArgumentHelpers.ThrowIfNull(json);
        try {
            var plan = AutomationPlanJson.Deserialize(json);
            _steps.Clear();
            _steps.AddRange(plan.Steps);
            _name = plan.Name;
            _click = null;
            _input = null;
            _refs = _steps.Count;
            return new(true, null);
        }
        catch (JsonException ex) {
            return new(false, ex.Message);
        }
    }

    /// <summary>
    /// Moves the step at <paramref name="fromIndex" /> so it lands at <paramref name="toIndex" />. Pending typing is committed first, so the indexes match the drawer list.
    /// </summary>
    public void Move(int fromIndex, int toIndex)
    {
        FlushPending();
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= _steps.Count || toIndex >= _steps.Count || fromIndex == toIndex)
            return;

        var step = _steps[fromIndex];
        _steps.RemoveAt(fromIndex);
        _steps.Insert(toIndex, step);
    }

    /// <summary>Removes the step at <paramref name="index" />. Pending typing is committed first, so the index matches the drawer list.</summary>
    public void RemoveAt(int index)
    {
        FlushPending();
        if (index < 0 || index >= _steps.Count)
            return;

        _steps.RemoveAt(index);
    }

    private void OnClick(RecordedDomEvent evt)
    {
        if (evt.Locator is not ElementLocator locator || evt.ElementKey is not string key || string.IsNullOrWhiteSpace(key))
            return;

        CommitInputIfOther(key);
        if (_click != null)
            CommitClick();

        _click = new(key, locator);
    }

    private void OnInput(RecordedDomEvent evt)
    {
        if (evt.Locator is not ElementLocator locator || evt.ElementKey is not string key || string.IsNullOrWhiteSpace(key))
            return;

        if (_click != null && _click.Key == key)
            _click = null;
        else
            CommitClick();

        CommitInputIfOther(key);
        _input = new(key, locator, evt.Text ?? "");
    }

    private void OnBlur(RecordedDomEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.ElementKey)) {
            FlushPending();
            return;
        }

        if (_click != null && _click.Key == evt.ElementKey)
            CommitClick();
    }

    private void OnSelect(RecordedDomEvent evt)
    {
        FlushPending();
        if (evt.Locator is not ElementLocator locator || evt.Value is not string value || string.IsNullOrWhiteSpace(value))
            return;

        _steps.Add(new FindAndActAutomationStep(NextRef(), locator, new SelectByValueElementAction(value)));
    }

    private void OnKey(RecordedDomEvent evt)
    {
        if (evt.Locator is not ElementLocator locator || string.IsNullOrWhiteSpace(evt.Text))
            return;

        CommitInput();
        if (_click != null && evt.ElementKey != null && _click.Key == evt.ElementKey)
            _click = null;
        else
            CommitClick();

        _steps.Add(new FindAndActAutomationStep(NextRef(), locator, new SendKeysElementAction(evt.Text)));
    }

    private void OnNavigate(RecordedDomEvent evt)
    {
        FlushPending();
        if (evt.CausedByRecordedAction)
            return;

        if (evt.Url is not string url || string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            return;

        if (_steps.Count > 0 && _steps[^1] is NavigateAutomationStep previous && string.Equals(previous.Url, url, StringComparison.OrdinalIgnoreCase))
            return;

        _steps.Add(new NavigateAutomationStep(url));
    }

    private void OnPick(RecordedDomEvent evt)
    {
        FlushPending();
        if (evt.Pick == null || string.IsNullOrWhiteSpace(evt.Pick.VariableName))
            return;

        var pick = evt.Pick;
        if (pick.Repeating && pick.ParentLocator != null) {
            var tag = string.IsNullOrWhiteSpace(pick.DescendantTag) ? DefaultTag(pick.Tool) : pick.DescendantTag ?? DefaultTag(pick.Tool);
            var listRef = NextRef();
            _steps.Add(new FindElementsChainAutomationStep(listRef, new ElementLocatorChain(pick.ParentLocator, ElementLocator.TagName(tag))));
            _steps.Add(new ExtractSourcesAutomationStep(listRef, pick.VariableName, AttributesFor(pick)));
            return;
        }

        if (pick.TargetLocator == null)
            return;

        var elementRef = NextRef();
        _steps.Add(new FindElementAutomationStep(elementRef, pick.TargetLocator));
        if (pick.Tool is RecordedPickTool.Text)
            _steps.Add(new ExtractElementDataAutomationStep(elementRef, pick.VariableName, ElementDataExtractKind.Text));
        else
            _steps.Add(new ExtractElementDataAutomationStep(elementRef, pick.VariableName, ElementDataExtractKind.Attribute, AttributeForSingle(pick)));
    }

    private void OnInsert(RecordedDomEvent evt)
    {
        FlushPending();
        if (evt.Insert == null)
            return;

        var insert = evt.Insert;
        var variable = insert.VariableName;
        var directory = insert.Directory;
        switch (insert.StepType) {
            case "storeLiteral" when variable is not null && !string.IsNullOrWhiteSpace(variable):
                _steps.Add(new StoreLiteralAutomationStep(variable, insert.Value ?? ""));
                break;
            case "storePageUrl" when variable is not null && !string.IsNullOrWhiteSpace(variable):
                _steps.Add(new StorePageUrlAutomationStep(variable));
                break;
            case "storePageTitle" when variable is not null && !string.IsNullOrWhiteSpace(variable):
                _steps.Add(new StorePageTitleAutomationStep(variable));
                break;
            case "delay" when insert.DelayMilliseconds is > 0:
                _steps.Add(new DelayAutomationStep(insert.DelayMilliseconds.Value));
                break;
            case "storeStringList" when variable is not null && !string.IsNullOrWhiteSpace(variable) && insert.Values is { Count: > 0 }:
                _steps.Add(new StoreStringListAutomationStep(variable, insert.Values));
                break;
            case "captureNetwork" when variable is not null && !string.IsNullOrWhiteSpace(variable):
                var listen = insert.DelayMilliseconds is > 0 ? insert.DelayMilliseconds.Value : 3000;
                var pattern = string.IsNullOrWhiteSpace(insert.Value) ? null : insert.Value;
                _steps.Add(new CaptureNetworkAutomationStep(variable, pattern, listen));
                break;
            case "downloadUrls" when variable is not null && directory is not null && !string.IsNullOrWhiteSpace(variable) && !string.IsNullOrWhiteSpace(directory):
                _steps.Add(new DownloadUrlsToDirectoryAutomationStep(variable, directory));
                break;
            case "navigate" when !string.IsNullOrWhiteSpace(insert.Value):
                _steps.Add(new NavigateAutomationStep(insert.Value));
                break;
            case "reload":
                _steps.Add(new ReloadAutomationStep());
                break;
            case "findElement" when LocatorOf(insert) is { } found:
                _steps.Add(new FindElementAutomationStep(NextRef(), found));
                break;
            case "findAndAct" when LocatorOf(insert) is { } target && ActionOf(insert) is { } action:
                _steps.Add(new FindAndActAutomationStep(NextRef(), target, action));
                break;
            case "setViewport" when ViewportOf(insert) is { } size:
                _steps.Add(new SetViewportSizeAutomationStep(size.Width, size.Height));
                break;
            case "openTab":
                _steps.Add(new OpenNewTabAutomationStep(string.IsNullOrWhiteSpace(insert.Value) ? null : insert.Value));
                break;
            case "closeTab":
                _steps.Add(new CloseCurrentTabAutomationStep());
                break;
            case "switchTab" when int.TryParse(insert.Value, out var tabIndex) && tabIndex >= 0:
                _steps.Add(new SwitchToTabByIndexAutomationStep(tabIndex));
                break;
        }
    }

    private static ElementLocator? LocatorOf(RecordedInsert insert)
    {
        if (insert.LocatorKind is not { } kind || string.IsNullOrWhiteSpace(insert.Value))
            return null;

        return new(kind, insert.Value);
    }

    private static ElementAction? ActionOf(RecordedInsert insert) => insert.Action switch {
        "click" => new ClickElementAction(),
        "input" => new InputTextElementAction(insert.Text ?? "", true),
        "clear" => new ClearElementAction(),
        _ => null
    };

    private static (int Width, int Height)? ViewportOf(RecordedInsert insert)
    {
        if (string.IsNullOrWhiteSpace(insert.Value))
            return null;

        var parts = insert.Value.Split('x', 'X');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
            return null;

        if (width <= 0 || height <= 0)
            return null;

        return (width, height);
    }

    private void FlushPending()
    {
        CommitInput();
        CommitClick();
    }

    private void CommitInputIfOther(string key)
    {
        if (_input != null && _input.Key != key)
            CommitInput();
    }

    private void CommitInput()
    {
        if (_input == null)
            return;

        _steps.Add(MakeInput(_input));
        _input = null;
    }

    private void CommitClick()
    {
        if (_click == null)
            return;

        _steps.Add(MakeClick(_click));
        _click = null;
    }

    private FindAndActAutomationStep MakeInput(PendingInput input) => new(NextRef(), input.Locator, new InputTextElementAction(input.Text, true));

    private FindAndActAutomationStep MakeClick(PendingClick click) => new(NextRef(), click.Locator, new ClickElementAction());

    private string NextRef() => $"el{++_refs}";

    private static string DefaultTag(RecordedPickTool tool) => tool == RecordedPickTool.Links ? "a" : "img";

    private static IReadOnlyList<string> AttributesFor(RecordedPick pick) => pick.Tool switch {
        RecordedPickTool.Links => ["href"],
        RecordedPickTool.Attribute when pick.AttributeName is string attribute && !string.IsNullOrWhiteSpace(attribute) => [attribute],
        RecordedPickTool.Text => ["text"],
        var _ => ImageAttributes
    };

    private static string AttributeForSingle(RecordedPick pick) => pick.Tool switch {
        RecordedPickTool.Links => "href",
        RecordedPickTool.Attribute when pick.AttributeName is string attribute && !string.IsNullOrWhiteSpace(attribute) => attribute,
        var _ => "src"
    };

    private sealed record PendingClick(string Key, ElementLocator Locator);

    private sealed record PendingInput(string Key, ElementLocator Locator, string Text);
}
