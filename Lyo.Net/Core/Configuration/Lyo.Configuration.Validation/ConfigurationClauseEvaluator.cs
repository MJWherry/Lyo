using System.Collections;
using System.Globalization;
using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Validation;
using Lyo.Validation.Models;
using Microsoft.Extensions.Configuration;

namespace Lyo.Configuration.Validation;

/// <summary>
/// An <see cref="IValidationClauseEvaluator" /> that maps a clause's dotted field paths to configuration keys, so a <see cref="ValidationSchema" /> written against
/// <c>Database.Port</c> can run straight against <see cref="IConfiguration" /> with no options type in the middle.
/// </summary>
/// <remarks>
/// Targets that are not an <see cref="IConfiguration" /> go to the inner evaluator unchanged, so one registration of this type covers both raw-key validation and
/// ordinary bound-object validation. Comparison semantics are never reimplemented here: each leaf condition is parsed into a <see cref="ConfigurationProbe{TValue}" /> and
/// re-asked of the inner evaluator, so operators behave the same as they do for entities. Only the tree walk and key lookup belong to this class.
/// </remarks>
public sealed class ConfigurationClauseEvaluator : IValidationClauseEvaluator
{
    private const string CountSuffix = ":Count";
    private const string ValueField = nameof(ConfigurationProbe<string>.Value);

    private readonly IValidationClauseEvaluator _inner;
    private readonly ConfigurationValidationOptions _options;

    /// <summary>Builds an evaluator that reads configuration keys and hands each leaf comparison to <paramref name="inner" />.</summary>
    /// <param name="inner">Evaluator used for leaf comparisons and for non-configuration targets, typically <see cref="WhereClauseServiceEvaluator" />.</param>
    /// <param name="options">Key-resolution behaviour. Defaults apply when this is null.</param>
    public ConfigurationClauseEvaluator(IValidationClauseEvaluator inner, ConfigurationValidationOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(inner);
        _inner = inner;
        _options = options ?? new ConfigurationValidationOptions();
    }

    /// <inheritdoc />
    public WhereClauseExplainResult Explain<T>(T value, WhereClause clause)
    {
        ArgumentHelpers.ThrowIfNull(clause);
        if (value is not IConfiguration configuration)
            return _inner.Explain(value, clause);

        var root = BuildNode(configuration, clause, "");
        var (blockingPath, failureSummary) = WhereClauseExplainAnalysis.ComputeBlockingFailure(root);
        var orBranches = root.Passed ? null : WhereClauseExplainAnalysis.CollectOrBranchOutcomes(root);
        return new(root, blockingPath, failureSummary, orBranches);
    }

    private WhereClauseExplainNode BuildNode(IConfiguration configuration, WhereClause node, string path)
    {
        var subPath = string.IsNullOrEmpty(path) ? "sub" : $"{path}/sub";
        switch (node) {
            case ConditionClause condition: {
                var raw = ReadValue(configuration, condition.Field);
                var primaryPassed = EvaluateCondition(condition, raw);
                var sub = condition.SubClause != null ? BuildNode(configuration, condition.SubClause, subPath) : null;
                return new() {
                    Passed = primaryPassed && (sub?.Passed ?? true),
                    Kind = WhereClauseExplainKind.Condition,
                    Path = path,
                    Description = condition.Description,
                    Field = condition.Field,
                    Comparison = condition.Comparison,
                    FilterValue = condition.Value,
                    ActualValueSummary = raw ?? "(not set)",
                    PrimaryPredicatePassed = sub != null ? primaryPassed : null,
                    SubClause = sub
                };
            }
            case GroupClause group: {
                var children = new WhereClauseExplainNode[group.Children.Count];
                for (var i = 0; i < group.Children.Count; i++)
                    children[i] = BuildNode(configuration, group.Children[i], string.IsNullOrEmpty(path) ? i.ToString(CultureInfo.InvariantCulture) : $"{path}/{i}");

                // An empty And is vacuously true; an empty Or has nothing that could succeed.
                var groupPassed = group.Operator == GroupOperatorEnum.Or ? Array.Exists(children, c => c.Passed) : Array.TrueForAll(children, c => c.Passed);
                var sub = group.SubClause != null ? BuildNode(configuration, group.SubClause, subPath) : null;
                return new() {
                    Passed = groupPassed && (sub?.Passed ?? true),
                    Kind = WhereClauseExplainKind.Group,
                    Path = path,
                    Description = group.Description,
                    GroupOperator = group.Operator,
                    Children = children,
                    SubClause = sub
                };
            }
            default:
                throw new InvalidQueryException($"Unknown where-clause node type: {node.GetType().Name}");
        }
    }

    /// <summary>Reads the configuration string for a clause field; a trailing <c>Count</c> segment means the child count of the named section.</summary>
    private string? ReadValue(IConfiguration configuration, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;

        var key = NormalizeKey(field);
        if (key.Length == 0)
            return null;

        if (configuration[key] is { } direct)
            return direct;

        if (!key.EndsWith(CountSuffix, StringComparison.OrdinalIgnoreCase))
            return null;

        var basePath = key.Substring(0, key.Length - CountSuffix.Length);
        if (basePath.Length == 0)
            return null;

        var section = configuration.GetSection(basePath);
        return section.Exists() ? section.GetChildren().Count().ToString(CultureInfo.InvariantCulture) : null;
    }

    private string NormalizeKey(string field)
    {
        var key = _options.TreatDotAsKeyDelimiter ? field.Replace('.', ':') : field;
        return key.Trim(':', ' ');
    }

    /// <summary>Parses the configuration string into the type implied by the rule's filter literal, then asks the inner evaluator to apply the operator.</summary>
    private bool EvaluateCondition(ConditionClause condition, string? raw)
    {
        var probeCondition = new ConditionClause(ValueField, condition.Comparison, condition.Value, condition.Description);
        return ProbeKindOf(condition.Value) switch {
            ProbeKind.Integer => Matches<long?>(raw, probeCondition),
            ProbeKind.Number => Matches<decimal?>(raw, probeCondition),
            ProbeKind.Boolean => Matches<bool?>(raw, probeCondition),
            ProbeKind.DateTime => Matches<DateTimeOffset?>(raw, probeCondition),
#if NET
            ProbeKind.DateOnly => Matches<DateOnly?>(raw, probeCondition),
            ProbeKind.TimeOnly => Matches<TimeOnly?>(raw, probeCondition),
#endif
            ProbeKind.Guid => Matches<Guid?>(raw, probeCondition),
            var _ => Matches<string>(raw, probeCondition)
        };
    }

    private bool Matches<TValue>(string? raw, ConditionClause probeCondition)
    {
        // A value that will not parse as TValue stays null, which fails positive operators and satisfies negative ones — the same shape as a missing key.
        var probe = new ConfigurationProbe<TValue> { Value = TypeConversion.TryConvertTo<TValue>(raw, out var parsed) ? parsed : default };
        return _inner.Explain(probe, probeCondition).Passed;
    }

    private static ProbeKind ProbeKindOf(object? filterValue)
        => filterValue switch {
            null or string => ProbeKind.String,
            bool => ProbeKind.Boolean,
            byte or sbyte or short or ushort or int or uint or long or ulong => ProbeKind.Integer,
            float or double or decimal => ProbeKind.Number,
            DateTime or DateTimeOffset => ProbeKind.DateTime,
#if NET
            DateOnly => ProbeKind.DateOnly,
            TimeOnly => ProbeKind.TimeOnly,
#endif
            Guid => ProbeKind.Guid,
            JsonElement element => FromJsonElement(element),
            IEnumerable items => FromFirstItem(items),
            var _ => ProbeKind.String
        };

    /// <summary>JSON-sourced schemas carry <see cref="JsonElement" /> literals, so the probe shape comes from the element's value kind.</summary>
    private static ProbeKind FromJsonElement(JsonElement element)
        => element.ValueKind switch {
            JsonValueKind.Number => element.TryGetInt64(out _) ? ProbeKind.Integer : ProbeKind.Number,
            JsonValueKind.True or JsonValueKind.False => ProbeKind.Boolean,
            JsonValueKind.Array => FromFirstJsonItem(element),
            var _ => ProbeKind.String
        };

    private static ProbeKind FromFirstJsonItem(JsonElement array)
    {
        foreach (var item in array.EnumerateArray()) {
            if (item.ValueKind != JsonValueKind.Null)
                return FromJsonElement(item);
        }

        return ProbeKind.String;
    }

    /// <summary>An <c>In</c> / <c>NotIn</c> list is homogeneous in practice, so the first non-null item picks the shape.</summary>
    private static ProbeKind FromFirstItem(IEnumerable items)
    {
        foreach (var item in items) {
            if (item != null)
                return ProbeKindOf(item);
        }

        return ProbeKind.String;
    }

    private enum ProbeKind
    {
        String, Integer, Number, Boolean, DateTime,
#if NET
        DateOnly, TimeOnly,
#endif
        Guid
    }
}
