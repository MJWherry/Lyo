namespace Lyo.Diff.Text;

/// <summary>Runs a Myers-style diff on tokenizer output with few allocations.</summary>
public interface ITextDiffService
{
    /// <summary>Diffs <paramref name="oldText" /> and <paramref name="newText" /> using <paramref name="options" /> (default tokenizer behavior).</summary>
    TextDiffResult Diff(string oldText, string newText, TextDiffOptions? options = null);

    /// <summary>Diffs a pre-tokenized pair using explicit options (tokens must follow the same <see cref="ITextTokenizer" /> rules).</summary>
    TextDiffResult Diff(string oldText, TextToken[] oldTokens, string newText, TextToken[] newTokens, TextDiffOptions? options = null);
}