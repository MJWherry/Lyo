using Lyo.Common.Metadata.Records;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.LyoType;

public partial class LyoTypeSelect
{
    private string _element = LyoTypeInfo.String.FullName;
    private LyoTypeCollectionShape _shape = LyoTypeCollectionShape.Value;
    private bool _pendingConcreteName;

    /// <summary>Stored type FullName (CLR catalog, non-CLR Lyo type, concrete enum, list/array, or custom).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Fired when the stored FullName changes.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    [Parameter]
    public Margin Margin { get; set; } = Margin.Dense;

    [Parameter]
    public bool Dense { get; set; } = true;

    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>If true, the picker includes Custom… plus a FullName field for unlisted types (when <see cref="ShowFullNameField" /> is also true).</summary>
    [Parameter]
    public bool ShowCustom { get; set; } = true;

    /// <summary>
    /// If true, Custom… and enum types show a CLR FullName field under the picker (config). Job/report tables pass false and render that field in the row expander.
    /// </summary>
    [Parameter]
    public bool ShowFullNameField { get; set; } = true;

    private bool CanWrap
        => _element != LyoTypeUi.CustomTypeToken && LyoTypeUi.CanWrapInCollection(LyoTypeInfo.FromName(_element));

    private bool NeedsFullNameField => _element == LyoTypeUi.CustomTypeToken || _element == LyoTypeInfo.Enum.FullName;

    private string FullNameFieldValue => LyoTypeUi.ConcreteFullNameDisplay(Value);

    protected override void OnParametersSet()
    {
        if (_pendingConcreteName && (string.IsNullOrWhiteSpace(Value) || string.Equals(Value, LyoTypeInfo.Enum.FullName, StringComparison.Ordinal)))
            return;

        SyncFromValue(Value);
    }

    private void SyncFromValue(string? fullName)
    {
        _pendingConcreteName = false;
        if (string.IsNullOrWhiteSpace(fullName)) {
            _element = LyoTypeInfo.String.FullName;
            _shape = LyoTypeCollectionShape.Value;
            return;
        }

        var known = LyoTypeInfo.FromName(fullName);
        if (known == LyoTypeInfo.Unknown) {
            _element = _element == LyoTypeInfo.Enum.FullName ? LyoTypeInfo.Enum.FullName : LyoTypeUi.CustomTypeToken;
            _shape = LyoTypeCollectionShape.Value;
            return;
        }

        if (known == LyoTypeInfo.Enum) {
            _element = LyoTypeInfo.Enum.FullName;
            _shape = LyoTypeCollectionShape.Value;
            return;
        }

        _shape = LyoTypeUi.CollectionShape(fullName);
        var elementName = LyoTypeUi.ElementFullName(fullName);
        var element = LyoTypeInfo.FromName(elementName);
        _element = element == LyoTypeInfo.Unknown ? LyoTypeUi.CustomTypeToken : element.FullName;
    }

    private async Task OnElementChanged(string value)
    {
        _element = value;
        if (value == LyoTypeUi.CustomTypeToken) {
            _shape = LyoTypeCollectionShape.Value;
            _pendingConcreteName = true;
            await ValueChanged.InvokeAsync("");
            return;
        }

        if (value == LyoTypeInfo.Enum.FullName) {
            _shape = LyoTypeCollectionShape.Value;
            var resolved = string.IsNullOrWhiteSpace(Value) ? null : LyoTypeInfo.TryResolveClrType(Value);
            if (resolved?.IsEnum == true)
                return;

            _pendingConcreteName = true;
            await ValueChanged.InvokeAsync(LyoTypeInfo.Enum.FullName);
            return;
        }

        var selected = LyoTypeInfo.FromName(value);
        if (!LyoTypeUi.CanWrapInCollection(selected))
            _shape = LyoTypeCollectionShape.Value;

        _pendingConcreteName = false;
        await ValueChanged.InvokeAsync(LyoTypeUi.ComposeFullName(value, _shape));
    }

    private Task OnShapeChanged(LyoTypeCollectionShape shape)
    {
        _shape = shape;
        return ValueChanged.InvokeAsync(LyoTypeUi.ComposeFullName(_element, _shape));
    }

    private Task OnFullNameChanged(string value)
        => ValueChanged.InvokeAsync(LyoTypeUi.ConcreteFullNameFromInput(_element, value));
}
