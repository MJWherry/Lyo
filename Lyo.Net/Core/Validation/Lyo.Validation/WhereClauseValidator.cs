using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Exceptions;
using Lyo.Result;

namespace Lyo.Validation;

/// <summary>
/// <see cref="IValidator{T}" /> that evaluates <see cref="ValidationSchema.Constraints" /> via <see cref="IValidationClauseEvaluator" /> and maps the explain tree with
/// <see cref="WhereClauseExplainResult.ToErrors" />.
/// </summary>
public sealed class WhereClauseValidator<T> : IValidator<T>
{
    private readonly IValidationClauseEvaluator _evaluator;
    private readonly ValidationSchema _schema;

    /// <summary>Creates a validator for <paramref name="schema" /> using <paramref name="evaluator" />.</summary>
    public WhereClauseValidator(ValidationSchema schema, IValidationClauseEvaluator evaluator)
    {
        ArgumentHelpers.ThrowIfNull(schema);
        ArgumentHelpers.ThrowIfNull(schema.Constraints);
        ArgumentHelpers.ThrowIfNull(evaluator);
        _schema = schema;
        _evaluator = evaluator;
    }

    /// <inheritdoc />
    public Result<T> Validate(T value)
    {
        if (value is null)
            return Result<T>.Failure("Validation target cannot be null", ValidationErrorCodes.NullValue);

        try {
            var explain = _evaluator.Explain(value, _schema.Constraints);
            var errors = explain.ToErrors(ToOverrides(_schema.Messages));
            return errors.Count == 0 ? Result<T>.Success(value) : Result<T>.Failure(errors);
        }
        catch (InvalidQueryException ex) {
            return Result<T>.Failure(ex.Message, ValidationErrorCodes.ValidationFailed);
        }
    }

    private static IReadOnlyDictionary<string, WhereClauseErrorOverride>? ToOverrides(IReadOnlyDictionary<string, ValidationMessage>? messages)
    {
        if (messages == null || messages.Count == 0)
            return null;

        var map = new Dictionary<string, WhereClauseErrorOverride>(messages.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in messages)
            map[kvp.Key] = new() { ErrorCode = kvp.Value.ErrorCode, ErrorMessage = kvp.Value.ErrorMessage };

        return map;
    }
}
