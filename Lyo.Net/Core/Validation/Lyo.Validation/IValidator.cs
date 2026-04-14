using Lyo.Common;

namespace Lyo.Validation;

/// <summary>Validates an instance of <typeparamref name="T" /> and returns a structured result.</summary>
public interface IValidator<T>
{
    /// <summary>Validates the supplied instance.</summary>
    Result<T> Validate(T value);
}