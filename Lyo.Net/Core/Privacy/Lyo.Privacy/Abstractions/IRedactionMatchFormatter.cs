using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Abstractions;

/// <summary>Optional: produce a custom replacement string for a match instead of <see cref="RedactionPolicy.Placeholder" />.</summary>
public interface IRedactionMatchFormatter
{
    /// <summary>Return null to use the policy placeholder.</summary>
    string? FormatReplacement(string input, RedactionSpan span);
}