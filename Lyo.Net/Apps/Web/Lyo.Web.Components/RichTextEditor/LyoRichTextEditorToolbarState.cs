namespace Lyo.Web.Components.RichTextEditor;

public sealed class LyoRichTextEditorToolbarState
{
    public string BlockTag { get; set; } = "p";

    public string FontFamily { get; set; } = "Arial, Helvetica, sans-serif";

    public string FontSize { get; set; } = "16px";

    public string CodeLanguage { get; set; } = "plaintext";

    public string ForeColor { get; set; } = "#000000";

    public string HighlightColor { get; set; } = "#ffff00";

    public bool IsBold { get; set; }

    public bool IsItalic { get; set; }

    public bool IsUnderline { get; set; }

    public bool IsStrikeThrough { get; set; }

    public bool IsOrderedList { get; set; }

    public bool IsUnorderedList { get; set; }

    public bool IsAlignLeft { get; set; }

    public bool IsAlignCenter { get; set; }

    public bool IsAlignRight { get; set; }

    public bool IsLink { get; set; }

    public bool CanUndo { get; set; }

    public bool CanRedo { get; set; }
}