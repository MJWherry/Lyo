namespace Lyo.Diff.Text;

/// <summary>Kind of a text-diff segment.</summary>
public enum TextDiffKind
{
    /// <summary>The same token(s) on both sides.</summary>
    Equal,

    /// <summary>Tokens that appear only in the new text.</summary>
    Insert,

    /// <summary>Tokens that appear only in the old text.</summary>
    Delete
}