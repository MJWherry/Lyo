using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Formatter.Web.Components;

/// <summary>
/// Formatter-typed parameter editor wired into <c>LyoTypeValueInput</c> through <c>ILyoValueEditorDescriptor</c>. Wraps <see cref="LyoFormatterTemplateEditor" />
/// with the JSON round-trip a parameter value needs, plus a collapsed live preview and a browsable list of tokens the template can use.
/// </summary>
/// <remarks>
/// Parameter values are stored as JSON, so a template arrives as a JSON string (<c>"Hello {Name}"</c>) and has to be unwrapped for editing and re-serialized on
/// every change. An empty template stores null rather than <c>""</c>, matching how the other editors treat an unset value.
/// <para>
/// The token list is the point of this component. Autocomplete only helps once the user knows to type <c>{</c> and roughly what to look for; the list makes the
/// whole context discoverable, which is the difference between a usable template field and one that requires reading the scheduler source.
/// </para>
/// </remarks>
public partial class LyoFormatterValueEditor : IDisposable
{
    [Inject]
    private IFormatterService Formatter { get; set; } = null!;

    /// <summary>Resolved via <c>GetService</c> so a host with no example context still gets a working editor.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    /// <summary>JSON payload that stores the template string.</summary>
    [Parameter]
    public string? Json { get; set; }

    /// <summary>Fired with the re-serialized template, or null when the template is empty.</summary>
    [Parameter]
    public EventCallback<string?> JsonChanged { get; set; }

    /// <summary>Caption shown above the template editor.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>When true, the template is locked and tokens cannot be appended.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>When true, an empty template shows a validation caption. The host still owns submit checks.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>View-supplied sample context, layered over the host default. See <see cref="LyoFormatterExampleContext.Merge" />.</summary>
    [Parameter]
    public object? Context { get; set; }

    private LyoFormatterLiveSession _session = null!;
    private IReadOnlyList<LyoFormatterContextEntry> _tokens = [];
    private string _loadedTemplate = "";
    private string? _loadedJson;
    private bool _hasLoadedJson;
    private object? _appliedContext;
    private bool _hasAppliedContext;
    private bool _showPreview;
    private bool _showTokens;

    /// <inheritdoc />
    public void Dispose()
    {
        _session.PropertyChanged -= OnSessionChanged;
        _session.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _session = new(Formatter);
        _session.PropertyChanged += OnSessionChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var changed = false;
        if (!_hasLoadedJson || !string.Equals(_loadedJson, Json, StringComparison.Ordinal)) {
            _hasLoadedJson = true;
            _loadedJson = Json;
            _loadedTemplate = UnwrapJson(Json);
            _session.Template = _loadedTemplate;
            changed = true;
        }

        if (!_hasAppliedContext || !ReferenceEquals(_appliedContext, Context)) {
            _hasAppliedContext = true;
            _appliedContext = Context;
            _session.Context = LyoFormatterExampleContext.Merge(Services.GetService<LyoFormatterExampleContext>(), Context);
            _tokens = LyoFormatterContextCatalog.Build(_session.Context);
            changed = true;
        }

        if (changed)
            _session.RefreshPreview();
    }

    private void TogglePreview() => _showPreview = !_showPreview;

    private void ToggleTokens() => _showTokens = !_showTokens;

    private void AppendToken(LyoFormatterContextEntry token)
    {
        var current = _session.Template;
        var separator = current.Length == 0 || char.IsWhiteSpace(current[^1]) ? "" : " ";
        _session.Template = current + separator + "{" + token.Path + "}";
    }

    private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(LyoFormatterLiveSession.Template), StringComparison.Ordinal))
            _ = CommitTemplateAsync();

        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Writes the edited template back out as JSON. Comparing against the template we loaded (rather than against <see cref="Json" />) means loading a value never
    /// looks like an edit, so opening a definition cannot mark it dirty.
    /// </summary>
    private async Task CommitTemplateAsync()
    {
        if (string.Equals(_session.Template, _loadedTemplate, StringComparison.Ordinal))
            return;

        _loadedTemplate = _session.Template;
        _loadedJson = ToJson(_loadedTemplate);
        await JsonChanged.InvokeAsync(_loadedJson);
    }

    /// <summary>Writes an edited template back to a parameter value. An empty template becomes null, matching how the other editors treat an unset value.</summary>
    internal static string? ToJson(string template) => template.Length == 0 ? null : JsonSerializer.Serialize(template);

    /// <summary>Reads the template out of a stored parameter value, accepting a raw (never-serialized) template from an older value or a hand-edited one.</summary>
    internal static string UnwrapJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "";

        try {
            return JsonSerializer.Deserialize<string>(json) ?? "";
        }
        catch (JsonException) {
            return json.Trim().Trim('"');
        }
    }
}
