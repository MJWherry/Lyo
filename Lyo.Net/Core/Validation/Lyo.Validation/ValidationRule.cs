using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Validation;

/// <summary>Delegate-backed implementation of a validation rule.</summary>
public sealed class ValidationRule<T> : IValidationRule<T>
{
    private readonly Func<T, IReadOnlyList<Error>> _validate;

    /// <summary>Creates a rule from the supplied validation delegate.</summary>
    public ValidationRule(Func<T, IReadOnlyList<Error>> validate)
    {
        ArgumentHelpers.ThrowIfNull(validate);
        _validate = validate;
    }

    /// <inheritdoc />
    public IReadOnlyList<Error> Validate(T value) => _validate(value);
}