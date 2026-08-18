using Lyo.Query.Models.Common;

namespace Lyo.Validation;

/// <summary>Evaluates a <see cref="WhereClause" /> against an in-memory instance and returns the explain tree used to build <see cref="Lyo.Result.Error" />s.</summary>
/// <remarks>
/// The default net10 implementation wraps <c>IWhereClauseService.ExplainMatch</c>. Do not reimplement comparison operators here — reuse the query engine.
/// </remarks>
public interface IValidationClauseEvaluator
{
    /// <summary>Explains whether <paramref name="value" /> satisfies <paramref name="clause" />.</summary>
    WhereClauseExplainResult Explain<T>(T value, WhereClause clause);
}
