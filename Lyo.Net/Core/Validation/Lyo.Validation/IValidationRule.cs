using Lyo.Common;

namespace Lyo.Validation;

/// <summary>Represents a single validation rule for <typeparamref name="T" />.</summary>
public interface IValidationRule<T>
{
    /// <summary>Validates the supplied instance and returns zero or more errors.</summary>
    IReadOnlyList<Error> Validate(T value);
}