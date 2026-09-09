using Lyo.Result;

namespace Lyo.Validation.Models;

/// <summary>One validation rule for <typeparamref name="T" />.</summary>
public interface IValidationRule<T>
{
    /// <summary>Validates the given instance and returns zero or more errors.</summary>
    IReadOnlyList<Error> Validate(T value);
}