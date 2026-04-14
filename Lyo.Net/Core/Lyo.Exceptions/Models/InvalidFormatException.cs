using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a value is in an invalid format.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class InvalidFormatException : ArgumentException
{
    /// <summary>Gets the valid format descriptions or examples, if provided.</summary>
    public IReadOnlyList<string> ValidFormats { get; }

    /// <summary>Gets the invalid value that caused this exception.</summary>
    public string? InvalidValue { get; }

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

    /// <summary>Initializes a new instance of the <see cref="InvalidFormatException" /> class.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="paramName">The name of the parameter that caused the exception.</param>
    /// <param name="invalidValue">The invalid value that caused the exception.</param>
    /// <param name="validFormats">The valid format descriptions or examples.</param>
    public InvalidFormatException(string message, string? paramName = null, string? invalidValue = null, params string[] validFormats)
        : base(message, paramName)
    {
        InvalidValue = invalidValue;
        ValidFormats = validFormats?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList().AsReadOnly() ?? Array.Empty<string>().ToList().AsReadOnly();
    }

    /// <summary>Initializes a new instance of the <see cref="InvalidFormatException" /> class.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="paramName">The name of the parameter that caused the exception.</param>
    /// <param name="invalidValue">The invalid value that caused the exception.</param>
    /// <param name="validFormats">The valid format descriptions or examples.</param>
    public InvalidFormatException(string message, Exception? innerException, string? paramName = null, string? invalidValue = null, params string[] validFormats)
        : base(message, paramName, innerException)
    {
        InvalidValue = invalidValue;
        ValidFormats = validFormats?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList().AsReadOnly() ?? Array.Empty<string>().ToList().AsReadOnly();
    }

    public override string ToString() => $"{base.ToString()} (Invalid Value: '{InvalidValue}', Valid Formats: [{string.Join(", ", ValidFormats)}])";
}