namespace Lyo.Diff.Text;

/// <summary>Half-open span <c>[Start, Start + Length)</c> into a source string.</summary>
public readonly record struct TextToken(int Start, int Length);