using System.Runtime.CompilerServices;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Services.WhereClause;

/// <summary>Static utilities for WhereClause tree operations. Shared by QueryService and ProjectionService.</summary>
public static class WhereClauseUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetWhereClauseTreeHash(Models.Common.WhereClause node)
        => node switch {
            ConditionClause c => c.SubClause != null ? $"C({c.Field}{c.Comparison}{c.Value})Sub({GetWhereClauseTreeHash(c.SubClause)})" : $"C({c.Field}{c.Comparison}{c.Value})",
            GroupClause l => l.SubClause != null
                ? $"L({l.Operator}[{string.Join(",", l.Children.Select(GetWhereClauseTreeHash))}])Sub({GetWhereClauseTreeHash(l.SubClause)})"
                : $"L({l.Operator}[{string.Join(",", l.Children.Select(GetWhereClauseTreeHash))}])",
            var _ => ""
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAnySubClause(Models.Common.WhereClause? node)
        => node switch {
            null => false,
            ConditionClause c => c.SubClause != null || HasAnySubClause(c.SubClause),
            GroupClause l => l.SubClause != null || l.Children.Any(HasAnySubClause),
            var _ => false
        };

    /// <summary>Extracts flat conditions from a WhereClause for projection-level filtering. Returns false if SubQueries or unsupported operators are present.</summary>
    public static bool TryExtractConditions(Models.Common.WhereClause? node, out List<ConditionClause> conditions, out GroupOperatorEnum op)
    {
        conditions = [];
        op = GroupOperatorEnum.And;
        if (node == null)
            return true;

        if (node is ConditionClause condition) {
            if (condition.SubClause != null)
                return false;

            conditions.Add(condition);
            return true;
        }

        if (node is GroupClause logical) {
            if (logical.SubClause != null)
                return false;

            if (logical.Operator != GroupOperatorEnum.And && logical.Operator != GroupOperatorEnum.Or)
                return false;

            op = logical.Operator;
            foreach (var child in logical.Children) {
                if (!TryExtractConditions(child, out var childConditions, out var _))
                    return false;

                conditions.AddRange(childConditions);
            }

            return true;
        }

        return false;
    }
}