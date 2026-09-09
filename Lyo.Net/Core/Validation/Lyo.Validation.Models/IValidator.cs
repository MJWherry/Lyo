using Lyo.Result;

namespace Lyo.Validation.Models;

/// <summary>Checks an instance of <typeparamref name="T" /> and returns a structured result.</summary>
public interface IValidator<T>
{
    /// <summary>Validates the given instance.</summary>
    Result<T> Validate(T value);
}