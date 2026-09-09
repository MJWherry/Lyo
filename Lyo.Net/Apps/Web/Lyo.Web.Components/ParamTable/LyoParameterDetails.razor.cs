using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;
using Lyo.Web.Components.LyoType;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Expanded detail editor for one parameter row: description, default value, and optionally the type picker and value-picker options. Shared by the table and
/// card layouts of <see cref="LyoParameterEditor" /> so both draw the same fields.
/// </summary>
/// <remarks>
/// Description, default value, and options are written straight onto <see cref="Item" /> and reported through <see cref="OnChanged" />. Type changes go out
/// through <see cref="TypeChanged" /> instead, because the host also resets incompatible values and auto-expands rows whose type needs a wide editor.
/// </remarks>
public partial class LyoParameterDetails
{
    /// <summary>Row being edited in place.</summary>
    [Parameter]
    [EditorRequired]
    public LyoParameterEditRow Item { get; set; } = null!;

    /// <summary>Draw the type picker here. The card layout does; the table layout keeps the picker in the collapsed row.</summary>
    [Parameter]
    public bool ShowType { get; set; }

    /// <summary>Show the value-picker options kind and editor (definition parameters).</summary>
    [Parameter]
    public bool ShowOptionsEditor { get; set; }

    /// <summary>
    /// Show the Literal / Expression toggle above the default (definition parameters). Schedule and trigger overrides are values, so they have no default channel.
    /// </summary>
    [Parameter]
    public bool ShowDefaultKind { get; set; }

    /// <summary>Required to draw Options-backed value selects. Typed input still works when null.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>Sibling parameter key to current value, for <c>{{Key}}</c> binding in query-backed options.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string?>? SiblingValues { get; set; }

    /// <summary>Caption under the default-value editor. Null uses a generic "callers supply the value" hint.</summary>
    [Parameter]
    public string? DefaultValueHint { get; set; }

    /// <summary>Height of the JSON editor surface for types that need a JSON tree.</summary>
    [Parameter]
    public string EditorSurfaceHeight { get; set; } = "220px";

    /// <summary>Sample data for autocomplete and live preview in a registered formatter editor. See <see cref="LyoTypeValueInput.FormatterContext" />.</summary>
    [Parameter]
    public object? FormatterContext { get; set; }

    /// <summary>Raised with the new stored type FullName.</summary>
    [Parameter]
    public EventCallback<string> TypeChanged { get; set; }

    /// <summary>Fired after any edit so the host can mark itself dirty.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>Resolved with <c>GetService</c> so a host without a formatter still edits templates; only the resolved preview is unavailable.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private string ResolvedDefaultHint => DefaultValueHint ?? "Optional definition default. Leave empty so callers supply the value.";

    /// <summary>Only treat the row as expression-authored where the toggle is offered, so hiding it can never strand a row on an editor it cannot switch away from.</summary>
    private bool UsesExpressionDefault => ShowDefaultKind && Item.UsesExpressionDefault;

    /// <summary>Registered template type, or the plain string editor when no formatter package is loaded.</summary>
    private string TemplateTypeName => LyoTypeUi.TemplateTypeName ?? LyoTypeInfo.String.FullName;

    /// <summary>The stored template is raw text; the editor round-trips a JSON parameter value, so it is quoted on the way in and unquoted on the way out.</summary>
    private string? DefaultTemplateJson => LyoParameterValueJson.Normalize(LyoTypeInfo.String.FullName, Item.DefaultTemplate);

    private string ExpressionHint => ExpressionError ?? ResolvedExpressionHint;

    /// <summary>
    /// Why the template cannot produce a value of the declared type, or null when it can. Rendering here uses the same policy the API applies on save, so the caption a user
    /// reads while authoring is the message that would have blocked the write.
    /// </summary>
    private string? ExpressionError
        => string.IsNullOrWhiteSpace(Item.DefaultTemplate) || TryResolveExpression(out _, out var error) ? null : error;

    private string ResolvedExpressionHint
    {
        get {
            if (string.IsNullOrWhiteSpace(Item.DefaultTemplate))
                return $"Template rendered when no value is supplied. Must resolve to a valid {Item.Type}.";

            return TryResolveExpression(out var value, out _)
                ? $"Resolves to {DisplayValue(value)} — valid {Item.Type}."
                : $"Template rendered when no value is supplied. Must resolve to a valid {Item.Type}.";
        }
    }

    /// <summary>
    /// Resolved value for the caption. JSON string quoting is presentation noise here, so it comes off for every type. unlike
    /// <see cref="LyoParameterValueJson.Unwrap" />, where stripping it changes what length and pattern constraints measure.
    /// </summary>
    private static string DisplayValue(string? json)
    {
        if (string.IsNullOrEmpty(json) || json![0] != '"')
            return json ?? "";

        try {
            return JsonSerializer.Deserialize<string>(json) ?? json;
        }
        catch (JsonException) {
            return json;
        }
    }

    private bool TryResolveExpression(out string? value, out string? error)
        => LyoParameterDefaults.TryResolve(
            new(Item.Key, Item.Type, DefaultKind: LyoParameterDefaultKind.Expression, DefaultTemplate: Item.DefaultTemplate), Item.Value,
            Services.GetService<LyoTemplateResolver>(), out value, out error);

    private Task OnTypeChanged(string type) => TypeChanged.InvokeAsync(type);

    private Task OnDefaultKindChanged(LyoParameterDefaultKind kind)
        => Item.DefaultKind == kind
            ? Task.CompletedTask
            : Set(() => {
                Item.DefaultKind = kind;
                if (kind == LyoParameterDefaultKind.Literal)
                    Item.DefaultTemplate = null;
            });

    private Task OnDefaultTemplateChanged(string? json)
        => Set(() => Item.DefaultTemplate = string.IsNullOrWhiteSpace(json) ? null : LyoParameterValueJson.Unwrap(LyoTypeInfo.String.FullName, json));

    private Task OnConcreteFullNameChanged(string value) => TypeChanged.InvokeAsync(LyoTypeUi.ConcreteFullNameFromInput(Item.Type, value));

    private Task OnDescriptionChanged(string value) => Set(() => Item.Description = value);

    private Task OnValueChanged(string? value) => Set(() => Item.Value = value);

    private Task OnOptionsKindChanged(ParameterOptionsKind? kind)
        => Item.EffectiveKind == kind ? Task.CompletedTask : OnOptionsChanged(ParameterOptionsJson.CreateDefaultForKind(kind));

    private Task OnOptionsChanged(string? options)
        => Set(() => {
            Item.Options = options;
            Item.AllowedValues = ParameterOptionsJson.ToAllowedValues(options, LyoTypeUi.ToListKind(Item.Type));
        });

    private async Task Set(Action apply)
    {
        apply();
        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync();
    }
}
