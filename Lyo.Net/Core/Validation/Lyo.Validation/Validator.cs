using Lyo.Common;

namespace Lyo.Validation;

/// <summary>Default implementation of <see cref="IValidator{T}" /> backed by a set of rules.</summary>
public sealed class Validator<T> : IValidator<T>
{
    private readonly IReadOnlyList<IValidationRule<T>> _rules;

    /// <summary>Creates a validator from the supplied rules.</summary>
    public Validator(IReadOnlyList<IValidationRule<T>> rules) => _rules = rules ?? [];

    /// <inheritdoc />
    public Result<T> Validate(T value)
    {
        if (value is null)
            return Result<T>.Failure("Validation target cannot be null", ValidationErrorCodes.NullValue);

        List<Error>? errors = null;
        foreach (var rule in _rules) {
            var ruleErrors = rule.Validate(value);
            if (ruleErrors.Count == 0)
                continue;

            errors ??= new(ruleErrors.Count);
            errors.AddRange(ruleErrors);
        }

        return errors == null || errors.Count == 0 ? Result<T>.Success(value) : Result<T>.Failure(errors);
    }
}