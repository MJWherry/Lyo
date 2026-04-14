using Lyo.Exceptions;

namespace Lyo.Diff.Text;

/// <inheritdoc />
public sealed class TextDiffService(ITextTokenizer tokenizer) : ITextDiffService
{
    private static readonly TextDiffOptions DefaultOptions = new();

    /// <inheritdoc />
    public TextDiffResult Diff(string oldText, string newText, TextDiffOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(oldText, nameof(oldText));
        ArgumentHelpers.ThrowIfNull(newText, nameof(newText));
        options ??= DefaultOptions;
        var oldTokens = tokenizer.Tokenize(oldText, options);
        var newTokens = tokenizer.Tokenize(newText, options);
        return Diff(oldText, oldTokens, newText, newTokens, options);
    }

    /// <inheritdoc />
    public TextDiffResult Diff(string oldText, TextToken[] oldTokens, string newText, TextToken[] newTokens, TextDiffOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(oldText, nameof(oldText));
        ArgumentHelpers.ThrowIfNull(newText, nameof(newText));
        ArgumentHelpers.ThrowIfNull(oldTokens, nameof(oldTokens));
        ArgumentHelpers.ThrowIfNull(newTokens, nameof(newTokens));
        options ??= DefaultOptions;
        OperationHelpers.ThrowIf(
            oldTokens.Length > options.MaxTokensPerSide || newTokens.Length > options.MaxTokensPerSide,
            $"Token count exceeds {nameof(TextDiffOptions.MaxTokensPerSide)} ({options.MaxTokensPerSide}).");

        var chunks = MyersDiffCalculator.Compute(oldText, oldTokens, newText, newTokens);
        return new(oldText, newText, chunks.ToArray());
    }
}