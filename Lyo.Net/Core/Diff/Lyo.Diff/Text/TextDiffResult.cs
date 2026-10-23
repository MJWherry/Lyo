using System.Diagnostics;

namespace Lyo.Diff.Text;

/// <summary>Outcome of comparing two strings with a tokenizer-based Myers diff.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TextDiffResult(string OldText, string NewText, TextDiffChunk[] Chunks);