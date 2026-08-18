#if NET
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Validation;

/// <summary>net10 adapter: <see cref="IValidationClauseEvaluator" /> over <see cref="IWhereClauseService.ExplainMatch{TEntity}" />.</summary>
public sealed class WhereClauseServiceEvaluator : IValidationClauseEvaluator
{
    private readonly IWhereClauseService _where;

    /// <summary>Creates an evaluator that forwards to <paramref name="where" />.</summary>
    public WhereClauseServiceEvaluator(IWhereClauseService where)
    {
        ArgumentHelpers.ThrowIfNull(where);
        _where = where;
    }

    /// <inheritdoc />
    public WhereClauseExplainResult Explain<T>(T value, WhereClause clause)
    {
        ArgumentHelpers.ThrowIfNull(clause);
        return _where.ExplainMatch(value, clause);
    }
}
#endif
