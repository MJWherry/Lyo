using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// One column as the card layout sees it: a label, the same cell markup the table draws, and whether it heads the card. Built by
/// <see cref="ProjectedColumnRegistry.GetCardColumns" /> from the columns a grid declared.
/// </summary>
public sealed record LyoGridCardColumn
{
    /// <summary>Projected field path or property path the column reads.</summary>
    public required string Field { get; init; }

    /// <summary>Column title as declared. Falls back to the field leaf through <see cref="Label" />.</summary>
    public string? Title { get; init; }

    /// <summary>Cell markup for a row, shared with the table so both layouts format values identically.</summary>
    public required RenderFragment<object?> Cell { get; init; }

    /// <summary>When true this column draws as the card heading instead of a labelled field.</summary>
    public bool IsTitle { get; init; }

    /// <summary>Whether the column can be sorted, which decides if it appears in the card layout's sort menu.</summary>
    public bool Sortable { get; init; }

    /// <summary>Display label: the declared title, or the last dotted segment of <see cref="Field" />.</summary>
    public string Label => !string.IsNullOrWhiteSpace(Title) ? Title!.Trim() : Field.LastIndexOf('.') is var dot and >= 0 ? Field[(dot + 1)..] : Field;
}
