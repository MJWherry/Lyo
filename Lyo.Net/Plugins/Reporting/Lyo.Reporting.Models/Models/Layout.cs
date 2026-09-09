using System.Diagnostics;

namespace Lyo.Reporting.Models.Models;

/// <summary>Page chrome, theme, and print hints applied by the report viewer and HTML/PDF renderer.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Layout
{
    /// <summary>Accent color as a CSS value (for example <c>#2563eb</c>). Used for header rules, callout borders, and progress fills.</summary>
    public string? AccentColor { get; set; }

    /// <summary>Page size hint: <c>Letter</c>, <c>A4</c>, or <c>Auto</c>.</summary>
    public string? PageSize { get; set; }

    /// <summary>Page orientation hint: <c>Portrait</c> or <c>Landscape</c>.</summary>
    public string? Orientation { get; set; }

    /// <summary>Visual theme: <c>Default</c>, <c>Compact</c>, or <c>Formal</c>.</summary>
    public string? Theme { get; set; }

    /// <summary>Optional header band text, shown above the title.</summary>
    public string? HeaderText { get; set; }

    /// <summary>Optional logo URL rendered in the header band. Must be an http(s), relative, or image data URL.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Optional watermark text drawn behind the body.</summary>
    public string? Watermark { get; set; }

    /// <summary>When true, the viewer appends a page-number placeholder to the footer (HTML is single-page; PDF pagination follows the HTML).</summary>
    public bool ShowPageNumbers { get; set; }

    /// <summary>CSS padding shorthand for the report body (for example <c>24px</c> or <c>1in</c>). When unset, the theme default is used.</summary>
    public string? Padding { get; set; }

    /// <summary>CSS padding-top; overrides the top of <see cref="Padding" /> when set.</summary>
    public string? PaddingTop { get; set; }

    /// <summary>CSS padding-right; overrides the right of <see cref="Padding" /> when set.</summary>
    public string? PaddingRight { get; set; }

    /// <summary>CSS padding-bottom; overrides the bottom of <see cref="Padding" /> when set.</summary>
    public string? PaddingBottom { get; set; }

    /// <summary>CSS padding-left; overrides the left of <see cref="Padding" /> when set.</summary>
    public string? PaddingLeft { get; set; }

    /// <summary>CSS margin shorthand for <c>@page</c> print/PDF (for example <c>1in</c> or <c>12mm</c>).</summary>
    public string? Margin { get; set; }

    /// <summary>CSS margin-top for <c>@page</c>; overrides the top of <see cref="Margin" /> when set.</summary>
    public string? MarginTop { get; set; }

    /// <summary>CSS margin-right for <c>@page</c>; overrides the right of <see cref="Margin" /> when set.</summary>
    public string? MarginRight { get; set; }

    /// <summary>CSS margin-bottom for <c>@page</c>; overrides the bottom of <see cref="Margin" /> when set.</summary>
    public string? MarginBottom { get; set; }

    /// <summary>CSS margin-left for <c>@page</c>; overrides the left of <see cref="Margin" /> when set.</summary>
    public string? MarginLeft { get; set; }

    /// <summary>
    /// Optional Blazor component FullName rendered as the HTML/PDF root instead of <c>ReportViewer</c>. Must resolve to an <c>IComponent</c> in a loaded assembly.
    /// </summary>
    public string? RootComponentType { get; set; }

    public override string ToString() => $"Layout: {Theme ?? "Default"} {PageSize ?? "Auto"} {Orientation ?? "Portrait"}";
}
