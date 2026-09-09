namespace Lyo.Diff.Text;

/// <summary>How strings are split before the Myers diff runs.</summary>
public enum TextTokenizeMode
{
    /// <summary>Each Unicode UTF-16 code unit becomes a token.</summary>
    Character,

    /// <summary>Split on line breaks after optional CR/LF normalization.</summary>
    Line,

    /// <summary>Split on whitespace runs (culture-sensitive word boundaries).</summary>
    Word
}