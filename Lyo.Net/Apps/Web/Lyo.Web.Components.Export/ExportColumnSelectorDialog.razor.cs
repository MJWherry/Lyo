using System.Reflection;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.Web.Components.Export;

public partial class ExportColumnSelectorDialog
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>When set, fills AvailableFields from type reflection. Ignored when AvailableFields is already provided.</summary>
    [Parameter]
    public Type? DataType { get; set; }

    /// <summary>Field names to show. Required when DataType is null. When DataType is set, taken from type properties.</summary>
    [Parameter]
    public IEnumerable<string>? AvailableFields { get; set; }

    [Parameter]
    public IReadOnlyList<FilterPropertyDefinition>? DisplayNameOverrides { get; init; }

    [Parameter]
    public bool AllowCustomColumns { get; set; } = true;

    /// <summary>
    /// Field paths (<see cref="ExportColumnItem.Value" />) that start <strong>unchecked</strong> (for example columns hidden in the grid).
    /// The user can still enable them for export.
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<string>? FieldsUncheckedByDefault { get; set; }

    /// <summary>Older name for <see cref="FieldsUncheckedByDefault" />.</summary>
    [Parameter]
    public IReadOnlyCollection<string>? DisabledExportFields { get; set; }

    private List<ExportColumnItem> AllItems { get; set; } = [];

    private int SelectedCount => AllItems.Count(p => p.IsSelected);

    private bool _showAddCustom;
    private string _customHeader = "";
    private string _customTemplate = "";

    private HashSet<string> _uncheckedByDefaultLookup = [];

    private int? _dragSourceIndex;
    private int? _dragOverIndex;

    protected override void OnInitialized()
    {
        var source = FieldsUncheckedByDefault ?? DisabledExportFields;
        _uncheckedByDefaultLookup = source?.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        var fields = GetAvailableFields().ToList();
        var lookup = DisplayNameOverrides?.ToDictionary(p => p.PropertyName, p => p.DisplayName ?? p.PropertyName, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, string>();
        AllItems = fields.Select((f, i) => {
                var uncheckedDefault = _uncheckedByDefaultLookup.Contains(f);
                return new ExportColumnItem {
                    Header = lookup.TryGetValue(f, out var display) ? display : FormatFieldAsHeader(f),
                    Value = f,
                    IsCustom = false,
                    UncheckedByDefault = uncheckedDefault,
                    IsSelected = !uncheckedDefault,
                    Order = i
                };
            })
            .ToList();
    }

    private static string GetRowLabelStyle(ExportColumnItem item) => item.IsSelected ? "font-weight: 500;" : "color: var(--mud-palette-text-disabled);";

    private string GetDropRowClass(int index) => _dragOverIndex == index && _dragSourceIndex is int d && d != index ? "export-col-drop-target" : "";

    private string GetDraggingRowStyle(int index) => _dragSourceIndex == index ? "opacity: 0.55;" : "";

    private void OnRowDragStart(DragEventArgs e, int index)
    {
        _dragSourceIndex = index;
        _dragOverIndex = null;
        e.DataTransfer?.EffectAllowed = "move";
    }

    private void OnDragEnterRow(int index)
    {
        if (_dragSourceIndex is int from && from != index)
            _dragOverIndex = index;
    }

    private void OnDragEnd()
    {
        _dragSourceIndex = null;
        _dragOverIndex = null;
    }

    private void OnRowDrop(int dropIndex)
    {
        if (_dragSourceIndex is not int fromIdx)
            return;

        OnDragEnd();
        if (fromIdx < 0 || fromIdx >= AllItems.Count)
            return;

        if (dropIndex < 0 || dropIndex >= AllItems.Count)
            return;

        if (fromIdx == dropIndex)
            return;

        var item = AllItems[fromIdx];
        AllItems.RemoveAt(fromIdx);
        var insertAt = dropIndex;
        if (insertAt > fromIdx)
            insertAt--;

        AllItems.Insert(insertAt, item);
        UpdateOrderIndexes();
        StateHasChanged();
    }

    private IEnumerable<string> GetAvailableFields()
    {
        if (AvailableFields != null)
            return AvailableFields.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f!.Trim()).Distinct();

        if (DataType != null) {
            var props = DataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props.Where(p => IsExportableType(p.PropertyType)).Select(p => p.Name);
        }

        return [];
    }

    private static string FormatFieldAsHeader(string field)
    {
        if (string.IsNullOrEmpty(field))
            return field;

        if (field.EndsWith(".Count", StringComparison.OrdinalIgnoreCase))
            return field[..^6].Replace(".", " ") + " Count";

        return field.Replace(".", " ");
    }

    private static bool IsExportableType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(decimal) || type == typeof(Guid) || type.IsEnum;
    }

    private void AddCustomColumn()
    {
        var header = _customHeader.Trim();
        var template = _customTemplate.Trim();
        if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(template))
            return;

        var item = new ExportColumnItem {
            Header = header,
            Value = template.Contains('{') ? template : $"{{{template}}}",
            IsCustom = true,
            UncheckedByDefault = false,
            IsSelected = true,
            Order = AllItems.Count
        };

        AllItems.Add(item);
        _customHeader = "";
        _customTemplate = "";
        _showAddCustom = false;
        StateHasChanged();
    }

    private void RemoveCustom(int index)
    {
        if (index >= 0 && index < AllItems.Count && AllItems[index].IsCustom) {
            AllItems.RemoveAt(index);
            UpdateOrderIndexes();
            StateHasChanged();
        }
    }

    private void ToggleSelection(int index, bool isSelected)
    {
        if (index >= 0 && index < AllItems.Count) {
            AllItems[index].IsSelected = isSelected;
            StateHasChanged();
        }
    }

    private void MoveUp(int index)
    {
        if (index > 0 && index < AllItems.Count) {
            var item = AllItems[index];
            AllItems.RemoveAt(index);
            AllItems.Insert(index - 1, item);
            UpdateOrderIndexes();
            StateHasChanged();
        }
    }

    private void MoveDown(int index)
    {
        if (index >= 0 && index < AllItems.Count - 1) {
            var item = AllItems[index];
            AllItems.RemoveAt(index);
            AllItems.Insert(index + 1, item);
            UpdateOrderIndexes();
            StateHasChanged();
        }
    }

    private void UpdateOrderIndexes()
    {
        for (var i = 0; i < AllItems.Count; i++)
            AllItems[i].Order = i;
    }

    private void SelectAll()
    {
        foreach (var item in AllItems)
            item.IsSelected = true;

        StateHasChanged();
    }

    private void SelectNone()
    {
        foreach (var item in AllItems)
            item.IsSelected = false;

        StateHasChanged();
    }

    private void Submit()
    {
        var result = AllItems.Where(p => p.IsSelected).OrderBy(p => p.Order).ToList();
        MudDialog.Close(DialogResult.Ok(result));
    }

    private void Cancel() => MudDialog.Cancel();

    public sealed class ExportColumnItem
    {
        public required string Header { get; init; }

        public required string Value { get; init; }

        public bool IsCustom { get; set; }

        /// <summary>Column is hidden in the grid. The dialog opens with this row unchecked; the user may still select it.</summary>
        public bool UncheckedByDefault { get; set; }

        public bool IsSelected { get; set; }

        public int Order { get; set; }
    }
}
