using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>A value did not match the expected format.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class InvalidFormatException : ArgumentException
{
    /// <summary>Accepted format descriptions or examples, when supplied.</summary>
    public IReadOnlyList<string> ValidFormats { get; }

    /// <summary>Value that failed format checks.</summary>
    public string? InvalidValue { get; }

    /// <inheritdoc />
    public override string Message {
        get {
            var baseMessage = base.Message;
            if (!string.IsNullOrWhiteSpace(InvalidValue))
                baseMessage += $" Invalid value: '{InvalidValue}'.";

            if (ValidFormats.Count > 0) {
                if (ValidFormats.Count == 1)
                    baseMessage += $" Valid format: {ValidFormats[0]}.";
                else
                    baseMessage += $" Valid formats: {string.Join(", ", ValidFormats)}.";
            }

            return baseMessage;
        }
    }

    /// <summary>Builds an <see cref="InvalidFormatException" /> for a bad value.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="paramName">Parameter that failed format checks.</param>
    /// <param name="invalidValue">Value that failed format checks.</param>
    /// <param name="validFormats">Accepted format descriptions or examples.</param>
    public InvalidFormatException(string message, string? paramName = null, string? invalidValue = null, params string[] validFormats)
        : base(message, paramName)
    {
        InvalidValue = invalidValue;
        ValidFormats = validFormats.Where(f => !string.IsNullOrWhiteSpace(f)).ToList().AsReadOnly();
    }

    /// <summary>Wraps <paramref name="innerException" /> with format details.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    /// <param name="paramName">Parameter that failed format checks.</param>
    /// <param name="invalidValue">Value that failed format checks.</param>
    /// <param name="validFormats">Accepted format descriptions or examples.</param>
    public InvalidFormatException(string message, Exception? innerException, string? paramName = null, string? invalidValue = null, params string[] validFormats)
        : base(message, paramName, innerException)
    {
        InvalidValue = invalidValue;
        ValidFormats = validFormats.Where(f => !string.IsNullOrWhiteSpace(f)).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public override string ToString() => $"{base.ToString()} (Invalid Value: '{InvalidValue}', Valid Formats: [{string.Join(", ", ValidFormats)}])";
}