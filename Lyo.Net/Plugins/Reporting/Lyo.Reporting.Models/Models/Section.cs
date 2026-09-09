using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Models;

/// <summary>A section inside a report.</summary>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(SectionJsonConverter))]
public sealed class Section
{
    /// <summary>Optional stable id for the designer and deep links.</summary>
    public string? Id { get; set; }

    /// <summary>Section title.</summary>
    public string? Title { get; set; }

    /// <summary>Section subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Section description.</summary>
    public string? Description { get; set; }

    /// <summary>Section order among sibling subsections.</summary>
    public int Order { get; set; }

    /// <summary>
    /// When set, the section is kept only if the bound parameters satisfy the condition. Forms: <c>Key</c> (non-empty), <c>!Key</c> (empty),
    /// <c>Key=value</c>, <c>Key!=value</c>.
    /// </summary>
    public string? VisibleWhen { get; set; }

    /// <summary>Ordered body controls (card, block, table, grid).</summary>
    public List<Control> Controls { get; set; } = [];

    /// <summary>Nested sections.</summary>
    public List<Section> Subsections { get; set; } = [];

    /// <summary>Custom CSS styles for this section.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    /// <summary>Whether this section starts collapsed.</summary>
    public bool Collapsed { get; set; }

    public override string ToString() => $"Section: {Title ?? "(Untitled)"} ({Controls.Count} controls, {Subsections.Count} subsections)";
}
