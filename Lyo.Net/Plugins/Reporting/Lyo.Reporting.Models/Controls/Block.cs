using System.Diagnostics;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Controls;

/// <summary>A content block (text, chart, heading, and the other <see cref="ContentType" /> values).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Block : Control
{
    /// <summary>Content type (text, html, list, and so on).</summary>
    public ContentType ContentType { get; set; } = ContentType.Text;

    /// <summary>Content text (heading, callout body, code, quote, or progress caption fallback).</summary>
    public string? Content { get; set; }

    /// <summary>List items (used by list, numbered list, and key-value content types). Key-value pairs are stored as <c>label|value</c>.</summary>
    public List<string>? ListItems { get; set; }

    /// <summary>Optional caption (image, progress, figure).</summary>
    public string? Caption { get; set; }

    /// <summary>Image URL or other source. Prefer this over putting a URL in <see cref="Content" />.</summary>
    public string? Source { get; set; }

    /// <summary>Alternate text for images.</summary>
    public string? Alt { get; set; }

    /// <summary>Heading level 1–6, or progress percent 0–100 when <see cref="ContentType" /> is <see cref="ContentType.Progress" />.</summary>
    public int? Level { get; set; }

    /// <summary>Callout tone: <c>info</c>, <c>success</c>, <c>warning</c>, or <c>error</c>.</summary>
    public string? Tone { get; set; }

    /// <summary>
    /// When set, the block is kept only if the bound parameters satisfy the condition. Same grammar as <see cref="Section.VisibleWhen" />.
    /// </summary>
    public string? VisibleWhen { get; set; }

    /// <summary>CLR FullName of an <c>IComponent</c> when <see cref="ContentType" /> is <see cref="ContentType.Component" />.</summary>
    public string? ComponentType { get; set; }

    /// <summary>Maps component <c>[Parameter]</c> names to report param keys or literals.</summary>
    public Dictionary<string, ComponentBinding> ParameterBindings { get; set; } = [];

    /// <summary>Structured Chart.js kind. When set, the viewer emits a canvas from series data instead of sanitizing <see cref="Content" /> HTML.</summary>
    public ChartKind? ChartKind { get; set; }

    /// <summary>Static series versus parameter or query rows. Null means static (or legacy HTML in <see cref="Content" />).</summary>
    public DataSourceKind? DataSourceKind { get; set; }

    /// <summary>Parameter key holding row JSON when <see cref="DataSourceKind" /> is <see cref="Controls.DataSourceKind.FromParameter" />.</summary>
    public string? DataParameterKey { get; set; }

    /// <summary>Dataset column used as chart labels when filling from a parameter.</summary>
    public string? ChartLabelField { get; set; }

    /// <summary>Dataset column used as chart values when filling from a parameter.</summary>
    public string? ChartValueField { get; set; }

    public override string ToString() => $"Block: {ContentType} ({Content?.Length ?? 0} chars)";
}

/// <summary>Kind of content in a <see cref="Block" />.</summary>
public enum ContentType
{
    Text,
    Html,
    List,
    NumberedList,
    Code,
    Quote,
    Chart,
    Image,
    Divider,
    Callout,
    PageBreak,
    Spacer,
    Progress,
    Heading,
    KeyValue,
    Component,
    Badge,
    Signature,
    TableOfContents,
    Timeline,
    Address,
    Totals,
    Checkbox,
    Notes
}

/// <summary>Chart.js type used when <see cref="Block.ChartKind" /> is set. Sparkline and Gauge are layout variants of line and doughnut.</summary>
public enum ChartKind
{
    Bar,
    Line,
    Doughnut,
    Pie,
    Sparkline,
    Gauge
}
