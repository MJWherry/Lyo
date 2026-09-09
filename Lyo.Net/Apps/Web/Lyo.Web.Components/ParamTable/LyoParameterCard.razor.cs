using Lyo.Api.Client;
using Lyo.Web.Components.LyoType;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// One parameter rendered as a card for <see cref="LyoParameterLayout.Cards" />. The header wraps instead of overflowing, so this layout survives narrow
/// viewports and rich value editors that a fixed table column cannot hold.
/// </summary>
public partial class LyoParameterCard
{
    /// <summary>Row being edited in place.</summary>
    [Parameter]
    [EditorRequired]
    public LyoParameterEditRow Item { get; set; } = null!;

    /// <summary>True when the detail editor is showing under the header.</summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>Show the drag handle, order number, and move up/down buttons.</summary>
    [Parameter]
    public bool AllowReorder { get; set; }

    /// <summary>False for the first card, so Move up is disabled.</summary>
    [Parameter]
    public bool CanMoveUp { get; set; }

    /// <summary>False for the last card, so Move down is disabled.</summary>
    [Parameter]
    public bool CanMoveDown { get; set; }

    /// <summary>True while this card is the drop target of an in-flight drag.</summary>
    [Parameter]
    public bool IsDropTarget { get; set; }

    /// <summary>True while this card is the one being dragged.</summary>
    [Parameter]
    public bool IsDragging { get; set; }

    /// <summary>Show the Required toggle.</summary>
    [Parameter]
    public bool ShowRequired { get; set; }

    /// <summary>Show the Encrypt toggle.</summary>
    [Parameter]
    public bool ShowEncrypt { get; set; }

    /// <summary>Show the Enabled toggle.</summary>
    [Parameter]
    public bool ShowEnabled { get; set; }

    /// <summary>Show the value-picker options kind and editor in the detail body.</summary>
    [Parameter]
    public bool ShowOptionsEditor { get; set; }

    /// <summary>Show the Literal / Expression default toggle in the detail body (definition parameters).</summary>
    [Parameter]
    public bool ShowDefaultKind { get; set; }

    /// <summary>Declared parameters this row may override; turns the key field into a select.</summary>
    [Parameter]
    public IReadOnlyList<LyoParameterEditRow>? InheritFrom { get; set; }

    /// <summary>Required to draw Options-backed value selects.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>Sibling parameter key to current value, for <c>{{Key}}</c> binding in query-backed options.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string?>? SiblingValues { get; set; }

    /// <summary>Caption under the default-value editor.</summary>
    [Parameter]
    public string? DefaultValueHint { get; set; }

    /// <summary>Sample data for autocomplete and live preview in a registered formatter editor. See <see cref="LyoTypeValueInput.FormatterContext" />.</summary>
    [Parameter]
    public object? FormatterContext { get; set; }

    /// <summary>Raised with the new key.</summary>
    [Parameter]
    public EventCallback<string?> KeyChanged { get; set; }

    /// <summary>Raised with the new stored type FullName.</summary>
    [Parameter]
    public EventCallback<string> TypeChanged { get; set; }

    /// <summary>Raised with the new Required value.</summary>
    [Parameter]
    public EventCallback<bool> RequiredChanged { get; set; }

    /// <summary>Raised with the new Encrypt value.</summary>
    [Parameter]
    public EventCallback<bool> EncryptChanged { get; set; }

    /// <summary>Raised with the new Enabled value.</summary>
    [Parameter]
    public EventCallback<bool> EnabledChanged { get; set; }

    /// <summary>Fired when the expand toggle is clicked.</summary>
    [Parameter]
    public EventCallback OnToggleExpanded { get; set; }

    /// <summary>Fired when the delete button is clicked.</summary>
    [Parameter]
    public EventCallback OnDelete { get; set; }

    /// <summary>Fired when Move up is clicked.</summary>
    [Parameter]
    public EventCallback OnMoveUp { get; set; }

    /// <summary>Fired when Move down is clicked.</summary>
    [Parameter]
    public EventCallback OnMoveDown { get; set; }

    /// <summary>Fired when a drag starts on this card's handle.</summary>
    [Parameter]
    public EventCallback<DragEventArgs> OnDragStart { get; set; }

    /// <summary>Fired when a drag enters this card.</summary>
    [Parameter]
    public EventCallback OnDragEnter { get; set; }

    /// <summary>Fired when a drag ends, whether or not it dropped here.</summary>
    [Parameter]
    public EventCallback OnDragEnd { get; set; }

    /// <summary>Fired when a dragged card is dropped on this one.</summary>
    [Parameter]
    public EventCallback OnDrop { get; set; }

    /// <summary>Fired after any edit so the host can mark itself dirty.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private string DropClass => IsDropTarget ? "lyo-param-card-drop-target" : "";

    private string? DragStyle => IsDragging ? "opacity: 0.55;" : null;
}
