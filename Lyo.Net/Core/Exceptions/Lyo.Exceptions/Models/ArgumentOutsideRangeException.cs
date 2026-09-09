using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>Thrown when a value is not within the expected range.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ArgumentOutsideRangeException : ArgumentOutOfRangeException
{
    /// <summary>Actual value that was out of range.</summary>
    public new IConvertible? ActualValue { get; }

    /// <summary>Inclusive minimum allowed value.</summary>
    public IConvertible? MinValue { get; }

    /// <summary>Inclusive maximum allowed value.</summary>
    public IConvertible? MaxValue { get; }

    /// <summary>Mints a new <see cref="ArgumentOutsideRangeException" />.</summary>
    /// <param name="paramName">Name of the parameter that caused the exception.</param>
    /// <param name="actualValue">Actual value that was out of range.</param>
    /// <param name="minValue">Inclusive minimum allowed value.</param>
    /// <param name="maxValue">Inclusive maximum allowed value.</param>
    /// <param name="message">Optional override for the exception message. A default is generated when null.</param>
    public ArgumentOutsideRangeException(string? paramName, IConvertible? actualValue, IConvertible? minValue, IConvertible? maxValue, string? message = null)
        : base(paramName, message ?? $"Value ({actualValue ?? "NULL"}) is not in the allowed range [{minValue ?? "Unspecified"}, {maxValue ?? "Unspecified"}].")
    {
        ActualValue = actualValue;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    /// <inheritdoc />
    public override string ToString() => $"{base.ToString()} (Actual: {ActualValue}, Range: [{MinValue}, {MaxValue}])";
}