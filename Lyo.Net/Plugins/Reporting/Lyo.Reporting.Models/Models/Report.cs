using System.Diagnostics;

namespace Lyo.Reporting.Models.Models;

/// <summary>A complete report with metadata, sections, and content.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Report<T>
{
    /// <summary>Report title.</summary>
    public string? Title { get; set; }

    /// <summary>Report subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Report description.</summary>
    public string? Description { get; set; }

    /// <summary>Custom metadata attached to the report.</summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];

    /// <summary>Report parameters or options (typed bag serialized into composition JSON).</summary>
    public T? Parameters { get; set; }

    /// <summary>Design-time parameter schema and example values used by the workbench and <c>{Key}</c> interpolation.</summary>
    public List<ParameterSpec> ParameterSpecs { get; set; } = [];

    /// <summary>Page chrome, theme, and print hints. Null is treated as the default layout.</summary>
    public Layout Layout { get; set; } = new();

    /// <summary>Sections in the report.</summary>
    public List<Section> Sections { get; set; } = [];

    /// <summary>Report footer text.</summary>
    public string? Footer { get; set; }

    /// <summary>Custom CSS styles for the report.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"Report: {Title ?? "(Untitled)"} ({Sections.Count} sections)";
}
