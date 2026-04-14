using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a value is not within the expected range.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ArgumentOutsideRangeException : ArgumentOutOfRangeException
{
    /// <summary>Gets the actual value that was out of range.</summary>
    public new IConvertible? ActualValue { get; }

    /// <summary>Gets the minimum allowed value (inclusive).</summary>
    public IConvertible? MinValue { get; }

    /// <summary>Gets the maximum allowed value (inclusive).</summary>
    public IConvertible? MaxValue { get; }

    /// <summary>Initializes a new instance of the NotInRangeException class with a custom message.</summary>
    /// <param name="message">The error message</param>
    /// <param name="paramName">The name of the parameter that caused the exception</param>
    /// <param name="actualValue">The actual value that was out of range</param>
    /// <param name="minValue">The minimum allowed value (inclusive)</param>
    /// <param name="maxValue">The maximum allowed value (inclusive)</param>
    public ArgumentOutsideRangeException(string? paramName, IConvertible? actualValue, IConvertible? minValue, IConvertible? maxValue, string? message = null)
        : base(paramName, message ?? $"Value ({actualValue ?? "NULL"}) is not in the allowed range [{minValue ?? "Unspecified"}, {maxValue ?? "Unspecified"}].")
    {
        ActualValue = actualValue;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public override string ToString() => $"{base.ToString()} (Actual: {ActualValue}, Range: [{MinValue}, {MaxValue}])";
}