using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Abstractions;

/// <summary>Finds spans in text to replace with the policy placeholder. Earlier rules in a policy win overlapping characters.</summary>
public interface IRedactionRule
{
    RedactionKind Kind { get; }

    IEnumerable<RedactionSpan> EnumerateSpans(string input);
}