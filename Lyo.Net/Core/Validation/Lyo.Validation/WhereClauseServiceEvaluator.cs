using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Services.WhereClause;
using Lyo.Validation.Models;

namespace Lyo.Validation;

/// <summary>Adapter that implements <see cref="IValidationClauseEvaluator" /> by calling <see cref="IWhereClauseService.ExplainMatch{TEntity}" />.</summary>
public sealed class WhereClauseServiceEvaluator : IValidationClauseEvaluator
{
    private readonly IWhereClauseService _where;

    /// <summary>Mints an evaluator that forwards to <paramref name="where" />.</summary>
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
