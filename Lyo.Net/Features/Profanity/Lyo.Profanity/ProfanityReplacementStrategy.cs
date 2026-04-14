namespace Lyo.Profanity;

/// <summary>Strategy for replacing detected profanity in text.</summary>
public enum ProfanityReplacementStrategy
{
    /// <summary>Remove the profane word entirely (replace with empty string).</summary>
    Remove,

    /// <summary>Replace each character with a mask character (e.g. "word" → "****").</summary>
    ReplaceWithChar,

    /// <summary>Replace the entire word with a fixed placeholder (e.g. "word" → "***").</summary>
    ReplaceWithWord,

    /// <summary>Replace with asterisks matching the original length (alias for ReplaceWithChar using '*').</summary>
    Mask,

    /// <summary>Replace with a placeholder that preserves first/last character (e.g. "word" → "w**d").</summary>
    PreserveBoundary,

    /// <summary>Do not replace; only detect. FilteredText will equal input if HasProfanity is true.</summary>
    DetectOnly
}