namespace Lyo.Diff.Text;

/// <summary>One contiguous segment of a text diff, referencing spans in the original old and new strings.</summary>
/// <remarks>
/// For <see cref="TextDiffKind.Delete" />, <see cref="NewStart" /> and <see cref="NewLength" /> are zero. For <see cref="TextDiffKind.Insert" />, <see cref="OldStart" /> and
/// <see cref="OldLength" /> are zero.
/// </remarks>
public readonly record struct TextDiffChunk(TextDiffKind Kind, int OldStart, int OldLength, int NewStart, int NewLength);