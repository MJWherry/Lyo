namespace Lyo.Diff.Text;

/// <summary>Half-open range <c>[Start, Start + Length)</c> into a source string.</summary>
public readonly record struct TextToken(int Start, int Length);