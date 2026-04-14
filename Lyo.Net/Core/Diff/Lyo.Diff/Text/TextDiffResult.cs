namespace Lyo.Diff.Text;

/// <summary>Result of comparing two strings with a tokenizer-based Myers diff.</summary>
public readonly record struct TextDiffResult(string OldText, string NewText, TextDiffChunk[] Chunks);