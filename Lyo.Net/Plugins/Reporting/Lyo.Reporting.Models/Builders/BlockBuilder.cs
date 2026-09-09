using System.Diagnostics;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for blocks.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BlockBuilder
{
    private readonly Block _block = new();

    /// <summary>Sets the block's content type.</summary>
    public BlockBuilder SetContentType(ContentType contentType)
    {
        _block.ContentType = contentType;
        return this;
    }

    /// <summary>Sets the block's content text.</summary>
    public BlockBuilder SetContent(string content)
    {
        _block.Content = content;
        return this;
    }

    /// <summary>Sets the list items (used by list content types).</summary>
    public BlockBuilder SetListItems(params string[] items) => SetListItems((IEnumerable<string>)items);

    /// <summary>Sets the list items (used by list content types).</summary>
    public BlockBuilder SetListItems(IEnumerable<string> items)
    {
        _block.ListItems = items.ToList();
        return this;
    }

    /// <summary>Adds a CSS style on the block.</summary>
    public BlockBuilder AddStyle(string property, string value)
    {
        _block.Styles[property] = value;
        return this;
    }

    /// <summary>Sets the caption (image, progress, figure).</summary>
    public BlockBuilder SetCaption(string caption)
    {
        _block.Caption = caption;
        return this;
    }

    /// <summary>Sets the image (or other) source URL.</summary>
    public BlockBuilder SetSource(string source)
    {
        _block.Source = source;
        return this;
    }

    /// <summary>Sets image alternate text.</summary>
    public BlockBuilder SetAlt(string alt)
    {
        _block.Alt = alt;
        return this;
    }

    /// <summary>Sets heading level (1–6) or progress percent (0–100).</summary>
    public BlockBuilder SetLevel(int level)
    {
        _block.Level = level;
        return this;
    }

    /// <summary>Sets callout tone (<c>info</c>, <c>success</c>, <c>warning</c>, or <c>error</c>).</summary>
    public BlockBuilder SetTone(string tone)
    {
        _block.Tone = tone;
        return this;
    }

    /// <summary>Sets a visibility condition evaluated against bound parameters.</summary>
    public BlockBuilder SetVisibleWhen(string visibleWhen)
    {
        _block.VisibleWhen = visibleWhen;
        return this;
    }

    /// <summary>Overrides print keep-together. Null on the block still means the type default; this sets an explicit value.</summary>
    public BlockBuilder SetKeepTogether(bool keepTogether)
    {
        _block.KeepTogether = keepTogether;
        return this;
    }

    /// <summary>Builds and returns the finished block.</summary>
    public Block Build() => _block;

    public override string ToString() => $"BlockBuilder: {_block.ContentType} ({_block.Content?.Length ?? 0} chars)";
}
