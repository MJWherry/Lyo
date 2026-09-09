namespace Lyo.Profanity;

/// <summary>Replacing detected profanity in text strategy.</summary>
public enum ProfanityReplacementStrategy
{
    /// <summary>Drop the profane word entirely (replace with empty string).</summary>
    Remove,

    /// <summary>Mask each character with a mask character (e.g. "word" → "****").</summary>
    ReplaceWithChar,

    /// <summary>Swap the whole word for a fixed placeholder (e.g. "word" → "***").</summary>
    ReplaceWithWord,

    /// <summary>Mask with asterisks matching the original length (alias for ReplaceWithChar using '*').</summary>
    Mask,

    /// <summary>Swap in a placeholder that preserves first/last character (e.g. "word" → "w**d").</summary>
    PreserveBoundary,

    /// <summary>Detect only; do not replace. FilteredText will equal input if HasProfanity is true.</summary>
    DetectOnly
}