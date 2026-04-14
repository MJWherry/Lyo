using System.Diagnostics;

namespace Lyo.Web.Reporting.Models;

/// <summary>Represents a section within a report.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportSection
{
    /// <summary>Gets or sets the section title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the section subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the section description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the section order/sequence.</summary>
    public int Order { get; set; }

    /// <summary>Gets the list of columns in this section.</summary>
    public List<ReportColumn> Columns { get; set; } = [];

    /// <summary>Gets the list of grids/tables in this section.</summary>
    public List<ReportGrid> Grids { get; set; } = [];

    /// <summary>Gets the list of nested sections.</summary>
    public List<ReportSection> Subsections { get; set; } = [];

    /// <summary>Gets or sets custom content blocks.</summary>
    public List<ReportContentBlock> ContentBlocks { get; set; } = [];

    /// <summary>Gets or sets custom CSS styles for this section.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    /// <summary>Gets or sets whether this section should be collapsed by default.</summary>
    public bool Collapsed { get; set; }

    public override string ToString() => $"Section: {Title ?? "(Untitled)"} ({Columns.Count} columns, {Grids.Count} grids)";
}