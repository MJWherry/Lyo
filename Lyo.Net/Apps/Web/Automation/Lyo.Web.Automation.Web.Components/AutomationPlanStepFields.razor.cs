using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Automation.Web.Components;

/// <summary>Fields for one runner step. Every <c>type</c> on <see cref="AutomationStepDefinition" /> has a form here.</summary>
public partial class AutomationPlanStepFields : ComponentBase
{
    /// <summary>Action discriminators the runner accepts.</summary>
    public static readonly string[] ActionTypes = ["click", "inputText", "sendKeys", "clear", "submit", "selectByText", "selectByValue", "selectByIndex", "dropdown"];

    private AutomationStepDefinition? _loaded;
    private string _type = "";
    private string _name = "";
    private string _url = "";
    private string _refName = "";
    private string _parentRef = "";
    private ElementLocatorKind _locatorKind = ElementLocatorKind.CssSelector;
    private string _locatorValue = "";
    private string _actionType = "click";
    private string _actionText = "";
    private DropdownSelectionMode _dropdownMode = DropdownSelectionMode.Auto;
    private bool _clickTrigger = true;
    private string _scopeParent = "";
    private string _variable = "";
    private string _value = "";
    private string _sourceVariable = "";
    private int _delayMs = 500;
    private int _width = 1280;
    private int _height = 720;
    private int _jitter;
    private int _tabIndex;
    private string _tabKey = "";
    private string _urlPattern = "";
    private int _listenMs = 3000;
    private string _urlListVariable = "urls";
    private string _targetDirectory = "downloads";
    private string _filePrefix = "";
    private string _fromStep = "";
    private string _values = "";
    private string _attributes = "";
    private string _elementsList = "elements";
    private ElementDataExtractKind _extractKind = ElementDataExtractKind.Text;
    private string _attribute = "";
    private bool _append;
    private bool _resolve = true;
    private bool _dedupe = true;
    private bool _split = true;
    private string _method = "GET";
    private string _body = "";
    private string _statusVariable = "";
    private string _responseVariable = "";
    private string _savedPathVariable = "";
    private string _targetFile = "";
    private string _targetName = "";
    private string _prefix = "";
    private string _storedList = "";
    private string _service = "";
    private string _methodName = "";
    private bool _throwMissing = true;
    private string _resultVariable = "";
    private List<SegmentRow> _segments = [];
    private List<PairRow> _pairs = [];
    private List<SizeRow> _sizes = [];

    /// <summary>Step being edited.</summary>
    [Parameter]
    public AutomationStepDefinition? Step { get; set; }

    /// <summary>Raised with a replacement step after a field changes.</summary>
    [Parameter]
    public EventCallback<AutomationStepDefinition> StepChanged { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Step is null || ReferenceEquals(Step, _loaded))
            return;

        _loaded = Step;
        Load(Step);
    }

    private string? NameOrNull => string.IsNullOrWhiteSpace(_name) ? null : _name;

    private void Touch()
    {
        if (Step is null)
            return;

        var next = Build();
        _loaded = next;
        _ = StepChanged.InvokeAsync(next);
    }

    private void Load(AutomationStepDefinition step)
    {
        _type = AutomationPlanDraft.StepType(step);
        _name = step.Name ?? "";
        _segments = [new(ElementLocatorKind.CssSelector, "")];
        _pairs = [];
        _sizes = [new(1280, 720)];
        switch (step) {
            case NavigateAutomationStep navigate:
                _url = navigate.Url;
                break;
            case DelayAutomationStep delay:
                _delayMs = delay.DelayMilliseconds;
                break;
            case FindElementAutomationStep find:
                LoadLocator(find.RefName, find.Locator);
                break;
            case FindDescendantAutomationStep descendant:
                _parentRef = descendant.ParentRefName;
                LoadLocator(descendant.RefName, descendant.Locator);
                break;
            case ElementActionAutomationStep action:
                _refName = action.ElementRefName;
                LoadAction(action.Action);
                break;
            case FindAndActAutomationStep act:
                LoadLocator(act.RefName, act.Locator);
                LoadAction(act.Action);
                break;
            case FindElementChainAutomationStep chain:
                _refName = chain.RefName;
                LoadChain(chain.Chain);
                break;
            case FindAndActChainAutomationStep actChain:
                _refName = actChain.RefName;
                LoadChain(actChain.Chain);
                LoadAction(actChain.Action);
                break;
            case FindElementsChainAutomationStep elements:
                _refName = elements.RefName;
                LoadChain(elements.Chain);
                break;
            case CaptureNetworkAutomationStep capture:
                _variable = capture.VariableName;
                _urlPattern = capture.UrlPattern ?? "";
                _listenMs = capture.DurationMilliseconds;
                break;
            case SetViewportSizeAutomationStep viewport:
                _width = viewport.Width;
                _height = viewport.Height;
                break;
            case RandomizeViewportAutomationStep random:
                _jitter = random.JitterPixels;
                _sizes = (random.Sizes ?? []).Select(size => new SizeRow(size.Width, size.Height)).ToList();
                if (_sizes.Count == 0)
                    _sizes.Add(new(1280, 720));
                break;
            case SwitchToTabByIndexAutomationStep tab:
                _tabIndex = tab.TabIndex;
                break;
            case SwitchToTabByKeyAutomationStep key:
                _tabKey = key.TabKey;
                break;
            case OpenNewTabAutomationStep open:
                _url = open.Url ?? "";
                _variable = open.TabKeyVariableName ?? "";
                break;
            case ExtractElementDataAutomationStep extract:
                _refName = extract.ElementRefName;
                _variable = extract.VariableName;
                _extractKind = extract.Kind;
                _attribute = extract.AttributeName ?? "";
                break;
            case ExtractElementsListDataAutomationStep listExtract:
                _elementsList = listExtract.ElementsListRefName;
                _variable = listExtract.VariableName;
                _extractKind = listExtract.Kind;
                _attribute = listExtract.AttributeName ?? "";
                break;
            case WriteStringListToFileAutomationStep write:
                _variable = write.VariableName;
                _targetFile = write.FilePath;
                _append = write.Append;
                break;
            case DownloadUrlsToDirectoryAutomationStep download:
                _urlListVariable = download.UrlListVariableName;
                _targetDirectory = download.TargetDirectory;
                _filePrefix = download.FileNamePrefix ?? "";
                _fromStep = download.UrlListFromCompletedStepIndex?.ToString() ?? "";
                break;
            case StoreLiteralAutomationStep literal:
                _variable = literal.VariableName;
                _value = literal.Value;
                break;
            case StoreStringListAutomationStep list:
                _variable = list.VariableName;
                _values = string.Join('\n', list.Values);
                break;
            case StoreTemplateAutomationStep template:
                _variable = template.VariableName;
                _value = template.Template;
                break;
            case StoreStringListFromTemplateAutomationStep fromTemplate:
                _sourceVariable = fromTemplate.SourceVariableName;
                _variable = fromTemplate.VariableName;
                _value = fromTemplate.ItemTemplate;
                break;
            case StorePageUrlAutomationStep url:
                _variable = url.VariableName;
                break;
            case StorePageTitleAutomationStep title:
                _variable = title.VariableName;
                break;
            case HttpRequestAutomationStep http:
                _method = http.Method;
                _url = http.Url;
                _body = http.BodyTemplate ?? "";
                _responseVariable = http.ResponseBodyVariableName ?? "";
                _statusVariable = http.StatusCodeVariableName ?? "";
                _pairs = (http.Headers ?? []).Select(pair => new PairRow(pair.Key, pair.Value)).ToList();
                break;
            case DownloadFileAutomationStep file:
                _url = file.Url;
                _targetFile = file.TargetFilePath;
                _savedPathVariable = file.SavedFilePathVariableName ?? "";
                break;
            case ExtractSourcesAutomationStep sources:
                _variable = sources.VariableName;
                _elementsList = sources.ElementsListRefName;
                _attributes = string.Join(", ", sources.AttributeNames ?? []);
                _resolve = sources.ResolveRelativeUrls;
                _dedupe = sources.Deduplicate;
                _split = sources.SplitCommaSeparatedValues;
                break;
            case UpsertJsonRecordsAutomationStep upsert:
                _variable = upsert.RecordsJsonVariableName;
                _targetName = upsert.TargetName;
                break;
            case UploadDirectoryToFileStorageAutomationStep upload:
                _targetDirectory = upload.SourceDirectory;
                _prefix = upload.DestinationPrefix;
                _storedList = upload.StoredFileListVariableName ?? "";
                break;
            case InvokeDiMethodAutomationStep invoke:
                _service = invoke.ServiceType;
                _methodName = invoke.MethodName;
                _throwMissing = invoke.ThrowOnMissingMethod;
                _resultVariable = invoke.ResultVariableName ?? "";
                _pairs = (invoke.Arguments ?? []).Select(pair => new PairRow(pair.Key, pair.Value)).ToList();
                break;
        }
    }

    private AutomationStepDefinition Build()
    {
        var name = NameOrNull;
        return _type switch {
            "navigate" => new NavigateAutomationStep(_url, name),
            "delay" => new DelayAutomationStep(_delayMs, name),
            "reload" => new ReloadAutomationStep(name),
            "closeCurrentTab" => new CloseCurrentTabAutomationStep(name),
            "findElement" => new FindElementAutomationStep(_refName, Locator(), name),
            "findDescendant" => new FindDescendantAutomationStep(_parentRef, _refName, Locator(), name),
            "elementAction" => new ElementActionAutomationStep(_refName, Action(), name),
            "findAndAct" => new FindAndActAutomationStep(_refName, Locator(), Action(), name),
            "findElementChain" => new FindElementChainAutomationStep(_refName, Chain(), name),
            "findAndActChain" => new FindAndActChainAutomationStep(_refName, Chain(), Action(), name),
            "findElementsChain" => new FindElementsChainAutomationStep(_refName, Chain(), name),
            "captureNetwork" => new CaptureNetworkAutomationStep(_variable, _urlPattern, _listenMs, name),
            "setViewportSize" => new SetViewportSizeAutomationStep(_width, _height, name),
            "randomizeViewport" => new RandomizeViewportAutomationStep(_sizes.Select(size => new DesktopDisplaySize(size.Width, size.Height)).ToArray(), _jitter, name),
            "switchTabByIndex" => new SwitchToTabByIndexAutomationStep(_tabIndex, name),
            "switchTabByKey" => new SwitchToTabByKeyAutomationStep(_tabKey, name),
            "openNewTab" => new OpenNewTabAutomationStep(Blank(_url), Blank(_variable), name),
            "extractElementData" => new ExtractElementDataAutomationStep(_refName, _variable, _extractKind, Blank(_attribute), name),
            "extractElementsListData" => new ExtractElementsListDataAutomationStep(_elementsList, _variable, _extractKind, Blank(_attribute), name),
            "writeStringListToFile" => new WriteStringListToFileAutomationStep(_variable, _targetFile, _append, name),
            "downloadUrlsToDirectory" => new DownloadUrlsToDirectoryAutomationStep(_urlListVariable, _targetDirectory, Blank(_filePrefix), name, StepIndex()),
            "storeLiteral" => new StoreLiteralAutomationStep(_variable, _value, name),
            "storeStringList" => new StoreStringListAutomationStep(_variable, SplitLines(_values), name),
            "storeTemplate" => new StoreTemplateAutomationStep(_variable, _value, name),
            "storeStringListFromTemplate" => new StoreStringListFromTemplateAutomationStep(_sourceVariable, _variable, _value, name),
            "storePageUrl" => new StorePageUrlAutomationStep(_variable, name),
            "storePageTitle" => new StorePageTitleAutomationStep(_variable, name),
            "httpRequest" => new HttpRequestAutomationStep(_method, _url, Pairs(), Blank(_body), Blank(_responseVariable), Blank(_statusVariable), name),
            "downloadFile" => new DownloadFileAutomationStep(_url, _targetFile, Blank(_savedPathVariable), name),
            "extractSources" => new ExtractSourcesAutomationStep(_elementsList, _variable, SplitCsv(_attributes), _resolve, _dedupe, _split, name),
            "upsertJsonRecords" => new UpsertJsonRecordsAutomationStep(_variable, _targetName, name),
            "uploadDirectoryToFileStorage" => new UploadDirectoryToFileStorageAutomationStep(_targetDirectory, _prefix, Blank(_storedList), name),
            "invokeDiMethod" => new InvokeDiMethodAutomationStep(_service, _methodName, Pairs(), _throwMissing, Blank(_resultVariable), name),
            _ => Step!
        };
    }

    private void LoadLocator(string refName, ElementLocator locator)
    {
        _refName = refName;
        _locatorKind = locator.Kind;
        _locatorValue = locator.Value;
    }

    private void LoadChain(ElementLocatorChain chain)
        => _segments = chain.Segments.Select(segment => new SegmentRow(segment.Kind, segment.Value)).ToList();

    private void LoadAction(ElementAction action)
    {
        switch (action) {
            case InputTextElementAction input:
                _actionType = "inputText";
                _actionText = input.Text;
                break;
            case SendKeysElementAction keys:
                _actionType = "sendKeys";
                _actionText = keys.Keys;
                break;
            case ClearElementAction:
                _actionType = "clear";
                break;
            case SubmitElementAction:
                _actionType = "submit";
                break;
            case SelectByTextElementAction text:
                _actionType = "selectByText";
                _actionText = text.Text;
                break;
            case SelectByValueElementAction value:
                _actionType = "selectByValue";
                _actionText = value.Value;
                break;
            case SelectByIndexElementAction index:
                _actionType = "selectByIndex";
                _actionText = index.Index.ToString();
                break;
            case DropdownElementAction dropdown:
                _actionType = "dropdown";
                _dropdownMode = dropdown.Mode;
                _actionText = dropdown.SelectByText ?? dropdown.SelectByValue ?? dropdown.SelectByIndex?.ToString() ?? "";
                _locatorKind = dropdown.OptionLocator?.Kind ?? ElementLocatorKind.CssSelector;
                _locatorValue = dropdown.OptionLocator?.Value ?? "";
                _clickTrigger = dropdown.ClickTriggerFirst;
                _scopeParent = dropdown.ScopeParentRef ?? "";
                break;
            default:
                _actionType = "click";
                _actionText = "";
                break;
        }
    }

    private ElementLocator Locator() => new(_locatorKind, _locatorValue);

    private ElementLocatorChain Chain()
    {
        var segments = _segments.Where(segment => !string.IsNullOrWhiteSpace(segment.Value)).Select(segment => new ElementLocator(segment.Kind, segment.Value)).ToArray();
        if (segments.Length == 0)
            segments = [new ElementLocator(ElementLocatorKind.CssSelector, "#main")];
        return new ElementLocatorChain(segments);
    }

    private ElementAction Action()
        => _actionType switch {
            "inputText" => new InputTextElementAction(_actionText),
            "sendKeys" => new SendKeysElementAction(_actionText),
            "clear" => new ClearElementAction(),
            "submit" => new SubmitElementAction(),
            "selectByText" => new SelectByTextElementAction(_actionText),
            "selectByValue" => new SelectByValueElementAction(_actionText),
            "selectByIndex" => new SelectByIndexElementAction(int.TryParse(_actionText, out var index) ? index : 0),
            "dropdown" => new DropdownElementAction(_dropdownMode, _actionText, null, null, string.IsNullOrWhiteSpace(_locatorValue) ? null : Locator(), _clickTrigger, Blank(_scopeParent)),
            _ => new ClickElementAction()
        };

    private Dictionary<string, string>? Pairs()
    {
        var pairs = _pairs.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        return pairs.Count == 0 ? null : pairs;
    }

    private int? StepIndex() => int.TryParse(_fromStep, out var index) ? index : null;

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static string[] SplitLines(string text)
        => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string[] SplitCsv(string text)
        => text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private void AddSegment()
    {
        _segments.Add(new(ElementLocatorKind.CssSelector, ""));
        Touch();
    }

    private void AddPair()
    {
        _pairs.Add(new("", ""));
        Touch();
    }

    private void AddSize()
    {
        _sizes.Add(new(1280, 720));
        Touch();
    }

    private sealed class SegmentRow(ElementLocatorKind kind, string value)
    {
        public ElementLocatorKind Kind { get; set; } = kind;
        public string Value { get; set; } = value;
    }

    private sealed class PairRow(string key, string value)
    {
        public string Key { get; set; } = key;
        public string Value { get; set; } = value;
    }

    private sealed class SizeRow(int width, int height)
    {
        public int Width { get; set; } = width;
        public int Height { get; set; } = height;
    }
}
