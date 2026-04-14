using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Diff.Text;

/// <summary>Default tokenizer for <see cref="ITextDiffService" />.</summary>
public sealed class TextTokenizer : ITextTokenizer
{
    /// <inheritdoc />
    public TextToken[] Tokenize(string text, TextDiffOptions options)
    {
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (options.NormalizeLineEndings) {
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");
        }

        var tokens = options.Mode switch {
            TextTokenizeMode.Character => TokenizeCharacters(text),
            TextTokenizeMode.Line => TokenizeLines(text),
            TextTokenizeMode.Word => TokenizeWords(text, options.Culture),
            var _ => throw new ArgumentOutOfRangeException(nameof(options), options.Mode, null)
        };

        OperationHelpers.ThrowIf(
            tokens.Length > options.MaxTokensPerSide, $"Token count {tokens.Length} exceeds {nameof(TextDiffOptions.MaxTokensPerSide)} ({options.MaxTokensPerSide}).");

        return tokens;
    }

    private static TextToken[] TokenizeCharacters(string text)
    {
        if (text.Length == 0)
            return [];

        var tokens = new TextToken[text.Length];
        for (var i = 0; i < text.Length; i++)
            tokens[i] = new(i, 1);

        return tokens;
    }

    private static TextToken[] TokenizeLines(string text)
    {
        if (text.Length == 0)
            return [];

        var list = new List<TextToken>();
        var i = 0;
        while (i < text.Length) {
            var start = i;
            while (i < text.Length && text[i] != '\n')
                i++;

            list.Add(new(start, i - start));
            if (i < text.Length && text[i] == '\n')
                i++;
        }

        return list.ToArray();
    }

    private static TextToken[] TokenizeWords(string text, CultureInfo? culture)
    {
        if (text.Length == 0)
            return [];

        var list = new List<TextToken>();
        var i = 0;
        _ = culture;
        while (i < text.Length) {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;

            if (i >= text.Length)
                break;

            var start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
                i++;

            list.Add(new(start, i - start));
        }

        return list.Count == 0 ? [] : list.ToArray();
    }
}