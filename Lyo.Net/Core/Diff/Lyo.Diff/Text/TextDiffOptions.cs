using System.Globalization;

namespace Lyo.Diff.Text;

/// <summary>Tokenizer mode and safety limits for <see cref="ITextDiffService" />.</summary>
public sealed class TextDiffOptions
{
    /// <summary>How input strings are split into tokens before the diff.</summary>
    public TextTokenizeMode Mode { get; set; } = TextTokenizeMode.Character;

    /// <summary>Culture applied to word-boundary rules when <see cref="Mode" /> is <see cref="TextTokenizeMode.Word" />.</summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>When true, CR/LF sequences are normalized to LF before tokens are built.</summary>
    public bool NormalizeLineEndings { get; set; } = true;

    /// <summary>Token cap per side; exceeding it throws <see cref="InvalidOperationException" />.</summary>
    public int MaxTokensPerSide { get; set; } = 100_000;
}