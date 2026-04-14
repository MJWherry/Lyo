using System.Diagnostics;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Builders;

/// <summary>Fluent builder for constructing content blocks.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ContentBlockBuilder
{
    private readonly ReportContentBlock _block = new();

    /// <summary>Sets the content type.</summary>
    public ContentBlockBuilder SetContentType(ReportContentType contentType)
    {
        _block.ContentType = contentType;
        return this;
    }

    /// <summary>Sets the content text.</summary>
    public ContentBlockBuilder SetContent(string content)
    {
        _block.Content = content;
        return this;
    }

    /// <summary>Sets the list items (for list content types).</summary>
    public ContentBlockBuilder SetListItems(params string[] items) => SetListItems((IEnumerable<string>)items);

    /// <summary>Sets the list items (for list content types).</summary>
    public ContentBlockBuilder SetListItems(IEnumerable<string> items)
    {
        _block.ListItems = items.ToList();
        return this;
    }

    /// <summary>Adds a CSS style to the content block.</summary>
    public ContentBlockBuilder AddStyle(string property, string value)
    {
        _block.Styles[property] = value;
        return this;
    }

    /// <summary>Builds and returns the content block.</summary>
    public ReportContentBlock Build() => _block;

    public override string ToString() => $"ContentBlockBuilder: {_block.ContentType} ({_block.Content?.Length ?? 0} chars)";
}