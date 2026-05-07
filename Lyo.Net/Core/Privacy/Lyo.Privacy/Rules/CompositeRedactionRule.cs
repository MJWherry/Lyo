using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Rules;

/// <summary>
/// Delegates span detection to an ordered inner rule list. Reported kind is <see cref="RedactionKind.Composite" />; use <see cref="RedactionPolicyBuilder" /> when per-kind
/// counts matter.
/// </summary>
public sealed class CompositeRedactionRule : IRedactionRule
{
    /// <summary>Ordered inner rules (for diagnostics and policy export).</summary>
    public IReadOnlyList<IRedactionRule> InnerRules { get; }

    public CompositeRedactionRule(IEnumerable<IRedactionRule> rules)
    {
        var ruleList = rules.AsReadOnlyList();
        ArgumentHelpers.ThrowIfNullOrEmpty(ruleList);
        InnerRules = ruleList;
    }

    public RedactionKind Kind => RedactionKind.Composite;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (var r in InnerRules) {
            foreach (var s in r.EnumerateSpans(input))
                yield return new(s.Start, s.Length, Kind);
        }
    }
}