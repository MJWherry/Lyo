using System.Diagnostics;

namespace Lyo.Web.Reporting.Models;

/// <summary>Represents a complete report with metadata, sections, and content.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Report<T>
{
    /// <summary>Gets or sets the report title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the report subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the report description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets custom metadata associated with the report.</summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];

    /// <summary>Gets or sets the report parameters/options.</summary>
    public T? Parameters { get; set; }

    /// <summary>Gets the list of sections in the report.</summary>
    public List<ReportSection> Sections { get; set; } = [];

    /// <summary>Gets or sets the report footer text.</summary>
    public string? Footer { get; set; }

    /// <summary>Gets or sets custom CSS styles for the report.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"Report: {Title ?? "(Untitled)"} ({Sections.Count} sections)";
}