using System.Collections.Immutable;
using System.Text;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Internal;
using Lyo.Privacy.Metrics;
using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Text;

/// <summary>Default engine: earliest rule in a policy wins overlapping characters; contiguous masked regions emit one replacement per run.</summary>
public sealed class TextRedactor : ITextRedactor
{
    private readonly IMetrics _metrics;
    private readonly RedactionPolicy _policy;

    public TextRedactor(RedactionPolicy policy, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(policy);
        _policy = policy;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    public RedactionResult Redact(string? input)
    {
        using (_metrics.StartTimer(PrivacyMetricNames.TextDuration, PrivacyObservation.TagsForPolicy(_policy.Name))) {
            _metrics.IncrementCounter(PrivacyMetricNames.TextOperations, 1, PrivacyObservation.TagsForPolicy(_policy.Name));
            var result = RedactCore(input);
            PrivacyMetricsRecorder.RecordTextRedactionResult(_metrics, result, _policy.Name);
            return result;
        }
    }

    /// <inheritdoc />
    public RedactionResult Redact(ReadOnlySpan<char> input) => Redact(SpanRedaction.ToString(input));

    private RedactionResult RedactCore(string? input)
    {
        if (input is null)
            return RedactionResult.Empty(null, _policy.Name);

        if (input.Length == 0 || _policy.Rules.Count == 0)
            return RedactionResult.Empty(input, _policy.Name);

        var winningRule = new int[input.Length];
        var winningKind = new RedactionKind[input.Length];
        for (var x = 0; x < winningRule.Length; x++)
            winningRule[x] = -1;

        for (var ri = 0; ri < _policy.Rules.Count; ri++) {
            foreach (var span in _policy.Rules[ri].EnumerateSpans(input)) {
                if (span.Length <= 0 || span.Start < 0 || span.End > input.Length)
                    continue;

                for (var p = span.Start; p < span.End; p++) {
                    if (winningRule[p] >= 0 && ri >= winningRule[p])
                        continue;

                    winningRule[p] = ri;
                    winningKind[p] = span.Kind;
                }
            }
        }

        ClearNeverRedactRanges(input, winningRule);
        return EmitFromWinners(input, winningRule, winningKind);
    }

    private void ClearNeverRedactRanges(string input, int[] winningRule)
    {
        if (_policy.NeverRedactSubstrings.Count == 0)
            return;

        foreach (var literal in _policy.NeverRedactSubstrings) {
            if (literal.Length == 0)
                continue;

            var start = 0;
            while ((start = input.IndexOf(literal, start, StringComparison.Ordinal)) >= 0) {
                for (var p = start; p < start + literal.Length && p < winningRule.Length; p++)
                    winningRule[p] = -1;

                start++;
            }
        }
    }

    private RedactionResult EmitFromWinners(string input, int[] winningRule, RedactionKind[] winningKind)
    {
        var counts = new Dictionary<RedactionKind, int>();
        var sb = new StringBuilder(input.Length + 16);
        var i = 0;
        while (i < input.Length) {
            if (winningRule[i] < 0) {
                sb.Append(input[i]);
                i++;
                continue;
            }

            var runStart = i;
            var ruleIdx = winningRule[i];
            var runKind = winningKind[i];
            if (_policy.MergeAdjacentRuns) {
                while (i < input.Length && winningRule[i] == ruleIdx && winningKind[i] == runKind)
                    i++;
            }
            else
                i++;

            var span = new RedactionSpan(runStart, i - runStart, runKind);
            var rule = _policy.Rules[ruleIdx];
            var replacement = rule is IRedactionMatchFormatter mf ? mf.FormatReplacement(input, span) : null;
            replacement ??= _policy.Placeholder;
            counts.TryGetValue(runKind, out var c);
            counts[runKind] = c + 1;
            sb.Append(replacement);
        }

        return new(sb.ToString(), counts.ToImmutableDictionary(), input.Length, sb.Length, _policy.Name);
    }
}