namespace Lyo.Diff.Text;

/// <summary>Maps a string into <see cref="TextToken" /> ranges used for diffing.</summary>
public interface ITextTokenizer
{
    /// <summary>Splits <paramref name="text" /> according to <paramref name="options" />.</summary>
    TextToken[] Tokenize(string text, TextDiffOptions options);
}