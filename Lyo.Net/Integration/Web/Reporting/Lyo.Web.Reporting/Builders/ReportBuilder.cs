using System.Diagnostics;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Builders;

/// <summary>Fluent builder for constructing reports with support for sections, columns, grids, and content blocks.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportBuilder<T>
{
    private readonly Report<T> _report = new();

    /// <summary>Sets the report title.</summary>
    public ReportBuilder<T> SetTitle(string title)
    {
        _report.Title = title;
        return this;
    }

    /// <summary>Sets the report subtitle.</summary>
    public ReportBuilder<T> SetSubtitle(string subtitle)
    {
        _report.Subtitle = subtitle;
        return this;
    }

    /// <summary>Sets the report description.</summary>
    public ReportBuilder<T> SetDescription(string description)
    {
        _report.Description = description;
        return this;
    }

    /// <summary>Sets the report parameters/options.</summary>
    public ReportBuilder<T> SetParameters(T parameters)
    {
        _report.Parameters = parameters;
        return this;
    }

    /// <summary>Adds a metadata key-value pair.</summary>
    public ReportBuilder<T> AddMetadata(string key, object? value)
    {
        _report.Metadata[key] = value;
        return this;
    }

    /// <summary>Adds multiple metadata key-value pairs.</summary>
    public ReportBuilder<T> AddMetadata(Dictionary<string, object?> metadata)
    {
        foreach (var kvp in metadata)
            _report.Metadata[kvp.Key] = kvp.Value;

        return this;
    }

    /// <summary>Sets the report footer text.</summary>
    public ReportBuilder<T> SetFooter(string footer)
    {
        _report.Footer = footer;
        return this;
    }

    /// <summary>Adds a CSS style to the report.</summary>
    public ReportBuilder<T> AddStyle(string property, string value)
    {
        _report.Styles[property] = value;
        return this;
    }

    /// <summary>Adds a section to the report using a configuration action.</summary>
    public ReportBuilder<T> AddSection(Action<SectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        var section = builder.Build();
        section.Order = _report.Sections.Count + 1;
        _report.Sections.Add(section);
        return this;
    }

    /// <summary>Adds a section to the report using a section builder.</summary>
    public ReportBuilder<T> AddSection(SectionBuilder sectionBuilder)
    {
        var section = sectionBuilder.Build();
        section.Order = _report.Sections.Count + 1;
        _report.Sections.Add(section);
        return this;
    }

    /// <summary>Adds a section with a title.</summary>
    public ReportBuilder<T> AddSection(string title, Action<SectionBuilder>? configure = null)
        => AddSection(sb => {
            sb.SetTitle(title);
            configure?.Invoke(sb);
        });

    /// <summary>Builds and returns the report.</summary>
    public Report<T> Build() => _report;

    /// <summary>Creates a new ReportBuilder instance.</summary>
    public static ReportBuilder<T> New() => new();

    /// <summary>Creates a new ReportBuilder instance with initial parameters.</summary>
    public static ReportBuilder<T> New(T parameters) => new ReportBuilder<T>().SetParameters(parameters);

    public override string ToString() => $"ReportBuilder: {_report.Title ?? "(Untitled)"} ({_report.Sections.Count} sections)";
}