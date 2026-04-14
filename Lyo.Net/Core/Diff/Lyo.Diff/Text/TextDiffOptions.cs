using System.Globalization;

namespace Lyo.Diff.Text;

/// <summary>Controls tokenizer mode and safety limits for <see cref="ITextDiffService" />.</summary>
public sealed class TextDiffOptions
{
    /// <summary>How input strings are split into tokens before diffing.</summary>
    public TextTokenizeMode Mode { get; set; } = TextTokenizeMode.Character;

    /// <summary>Culture used for word-boundary rules when <see cref="Mode" /> is <see cref="TextTokenizeMode.Word" />.</summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>When true, CR/LF sequences are normalized to LF before tokenization.</summary>
    public bool NormalizeLineEndings { get; set; } = true;

    /// <summary>Maximum number of tokens on either side; exceeding this throws <see cref="InvalidOperationException" />.</summary>
    public int MaxTokensPerSide { get; set; } = 100_000;
}