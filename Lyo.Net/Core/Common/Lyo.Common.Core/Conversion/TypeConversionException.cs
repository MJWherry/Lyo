namespace Lyo.Common.Core.Conversion;

/// <summary>
/// Raised when <see cref="TypeConversion" /> cannot produce the requested target type. Subclasses <see cref="InvalidOperationException" /> so callers that already catch that type
/// keep working.
/// </summary>
public class TypeConversionException : InvalidOperationException
{
    /// <summary>Value that failed conversion, when known.</summary>
    public object? Value { get; }

    /// <summary>Runtime type of the failed value, when known.</summary>
    public Type? SourceType { get; }

    /// <summary>Requested target type, when known.</summary>
    public Type? TargetType { get; }

    /// <summary>Builds an exception with the default conversion-failed message.</summary>
    public TypeConversionException()
        : base("The value could not be converted to the target type.") { }

    /// <summary>Builds an exception with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public TypeConversionException(string message)
        : base(message) { }

    /// <summary>Builds an exception with <paramref name="message" /> and an inner exception.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public TypeConversionException(string message, Exception? innerException)
        : base(message, innerException) { }

    /// <summary>Builds an exception that records the failed value and target type.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="value">Value that could not be converted.</param>
    /// <param name="targetType">Requested target type.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public TypeConversionException(string message, object? value, Type? targetType, Exception? innerException = null)
        : base(message, innerException)
    {
        Value = value;
        SourceType = value?.GetType();
        TargetType = targetType;
    }
}