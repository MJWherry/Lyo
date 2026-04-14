namespace Lyo.Diff.Text;

/// <summary>Strategy for splitting strings before running the Myers diff.</summary>
public enum TextTokenizeMode
{
    /// <summary>Each Unicode UTF-16 code unit is a token.</summary>
    Character,

    /// <summary>Split on line breaks (after optional CR/LF normalization).</summary>
    Line,

    /// <summary>Split on runs of whitespace (culture-sensitive for word boundaries).</summary>
    Word
}