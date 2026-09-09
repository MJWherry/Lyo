using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Request;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for reports, with support for sections, cards, tables, grids, and blocks.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportBuilder<T>
{
    private readonly Report<T> _report = new();

    /// <summary>Sets the report's title.</summary>
    public ReportBuilder<T> SetTitle(string title)
    {
        _report.Title = title;
        return this;
    }

    /// <summary>Sets the report's subtitle.</summary>
    public ReportBuilder<T> SetSubtitle(string subtitle)
    {
        _report.Subtitle = subtitle;
        return this;
    }

    /// <summary>Sets the report's description.</summary>
    public ReportBuilder<T> SetDescription(string description)
    {
        _report.Description = description;
        return this;
    }

    /// <summary>Sets the report parameters or options.</summary>
    public ReportBuilder<T> SetParameters(T parameters)
    {
        _report.Parameters = parameters;
        return this;
    }

    /// <summary>Adds one metadata key-value pair.</summary>
    public ReportBuilder<T> AddMetadata(string key, object? value)
    {
        _report.Metadata[key] = value;
        return this;
    }

    /// <summary>Adds several metadata key-value pairs.</summary>
    public ReportBuilder<T> AddMetadata(Dictionary<string, object?> metadata)
    {
        foreach (var kvp in metadata)
            _report.Metadata[kvp.Key] = kvp.Value;

        return this;
    }

    /// <summary>Sets the report's footer text.</summary>
    public ReportBuilder<T> SetFooter(string footer)
    {
        _report.Footer = footer;
        return this;
    }

    /// <summary>Adds a CSS style on the report.</summary>
    public ReportBuilder<T> AddStyle(string property, string value)
    {
        _report.Styles[property] = value;
        return this;
    }

    /// <summary>Replaces the report layout.</summary>
    public ReportBuilder<T> SetLayout(Layout layout)
    {
        _report.Layout = layout;
        return this;
    }

    /// <summary>Configures the report layout.</summary>
    public ReportBuilder<T> SetLayout(Action<LayoutBuilder> configure)
    {
        var builder = new LayoutBuilder();
        configure(builder);
        _report.Layout = builder.Build();
        return this;
    }

    /// <summary>Adds a design-time parameter spec (schema plus optional example value for workbench preview).</summary>
    public ReportBuilder<T> AddParameter(string key, string? type = null, string? example = null, bool required = false, string? description = null, string? defaultValue = null)
    {
        _report.ParameterSpecs.Add(
            new() {
                Key = key,
                Type = type ?? Lyo.Common.Metadata.Records.LyoTypeInfo.String.FullName,
                ExampleValue = example,
                Required = required,
                Description = description,
                DefaultValue = defaultValue
            });
        return this;
    }

    /// <summary>Adds a design-time parameter spec.</summary>
    public ReportBuilder<T> AddParameter(ParameterSpec spec)
    {
        _report.ParameterSpecs.Add(spec);
        return this;
    }

    /// <summary>Adds a section via a configuration action.</summary>
    public ReportBuilder<T> AddSection(Action<SectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        var section = builder.Build();
        section.Order = _report.Sections.Count + 1;
        _report.Sections.Add(section);
        return this;
    }

    /// <summary>Adds a section from a section builder.</summary>
    public ReportBuilder<T> AddSection(SectionBuilder sectionBuilder)
    {
        var section = sectionBuilder.Build();
        section.Order = _report.Sections.Count + 1;
        _report.Sections.Add(section);
        return this;
    }

    /// <summary>Adds a titled section.</summary>
    public ReportBuilder<T> AddSection(string title, Action<SectionBuilder>? configure = null)
        => AddSection(sb => {
            sb.SetTitle(title);
            configure?.Invoke(sb);
        });

    /// <summary>Builds and returns the finished report.</summary>
    public Report<T> Build() => _report;

    /// <summary>Builds a create-definition request from the composition JSON and <see cref="Report{T}.ParameterSpecs" />.</summary>
    public ReportDefinitionReq ToDefinitionReq(string name, string? description = null, ReportFormat? defaultFormat = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return new() {
            Name = name.Trim(),
            Description = description,
            ReportDataJson = ReportJson.Serialize(_report),
            IsActive = true,
            DefaultFormat = defaultFormat,
            CreateParameters = _report.ParameterSpecs
                .Where(s => !string.IsNullOrWhiteSpace(s.Key))
                .Select(
                    s => new ReportDefinitionParameterReq {
                        Key = s.Key.Trim(),
                        Type = s.Type,
                        Value = s.DefaultValue ?? s.ExampleValue,
                        Description = s.Description,
                        Required = s.Required,
                        AllowedValues = s.AllowedValues
                    })
                .ToList()
        };
    }

    /// <summary>Creates a new ReportBuilder.</summary>
    public static ReportBuilder<T> New() => new();

    /// <summary>Creates a new ReportBuilder with initial parameters.</summary>
    public static ReportBuilder<T> New(T parameters) => new ReportBuilder<T>().SetParameters(parameters);

    public override string ToString() => $"ReportBuilder: {_report.Title ?? "(Untitled)"} ({_report.Sections.Count} sections)";
}