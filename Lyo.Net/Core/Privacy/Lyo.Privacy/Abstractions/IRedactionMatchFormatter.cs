using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Abstractions;

/// <summary>Optional hook that produces a custom replacement string for a match instead of <see cref="RedactionPolicy.Placeholder" />.</summary>
public interface IRedactionMatchFormatter
{
    /// <summary>Return null to fall back to the policy placeholder.</summary>
    string? FormatReplacement(string input, RedactionSpan span);
}