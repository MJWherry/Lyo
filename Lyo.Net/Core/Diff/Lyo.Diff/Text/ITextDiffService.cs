namespace Lyo.Diff.Text;

/// <summary>Computes a Myers-style diff on tokenizer output with minimal allocations.</summary>
public interface ITextDiffService
{
    /// <summary>Diffs <paramref name="oldText" /> and <paramref name="newText" /> using <paramref name="options" /> (default tokenizer behavior).</summary>
    TextDiffResult Diff(string oldText, string newText, TextDiffOptions? options = null);

    /// <summary>Diffs using an explicit options object and a pre-tokenized pair (must come from the same <see cref="ITextTokenizer" /> rules).</summary>
    TextDiffResult Diff(string oldText, TextToken[] oldTokens, string newText, TextToken[] newTokens, TextDiffOptions? options = null);
}