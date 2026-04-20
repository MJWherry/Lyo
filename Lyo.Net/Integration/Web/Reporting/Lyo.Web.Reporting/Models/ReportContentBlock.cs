using System.Diagnostics;

namespace Lyo.Web.Reporting.Models;

/// <summary>Represents a content block within a report section.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportContentBlock
{
    /// <summary>Gets or sets the content type (text, html, list, etc.).</summary>
    public ReportContentType ContentType { get; set; } = ReportContentType.Text;

    /// <summary>Gets or sets the content text.</summary>
    public string? Content { get; set; }

    /// <summary>Gets or sets the list items (for list content type).</summary>
    public List<string>? ListItems { get; set; }

    /// <summary>Gets or sets custom CSS styles for this content block.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"ContentBlock: {ContentType} ({Content?.Length ?? 0} chars)";
}

/// <summary>Defines the type of content in a report content block.</summary>
public enum ReportContentType
{
    Text,
    Html,
    List,
    NumberedList,
    Code,
    Quote,
    Chart
}