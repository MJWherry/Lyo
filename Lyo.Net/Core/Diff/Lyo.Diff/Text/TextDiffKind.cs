namespace Lyo.Diff.Text;

/// <summary>Classification of a text diff segment.</summary>
public enum TextDiffKind
{
    /// <summary>Same token(s) on both sides.</summary>
    Equal,

    /// <summary>Present only in the new text.</summary>
    Insert,

    /// <summary>Present only in the old text.</summary>
    Delete
}