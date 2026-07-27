namespace Lyo.Common.Conversion;

/// <summary>
/// Exception thrown when <see cref="TypeConversion" /> cannot convert a value to the requested target type.
/// Derives from <see cref="InvalidOperationException" /> so existing handlers that catch the previous exception type continue to work.
/// </summary>
public class TypeConversionException : InvalidOperationException
{
    /// <summary>Gets the value that could not be converted, if available.</summary>
    public object? Value { get; }

    /// <summary>Gets the runtime type of the value that could not be converted, if available.</summary>
    public Type? SourceType { get; }

    /// <summary>Gets the requested target type, if available.</summary>
    public Type? TargetType { get; }

    /// <summary>Initializes a new instance of the <see cref="TypeConversionException" /> class.</summary>
    public TypeConversionException()
        : base("The value could not be converted to the target type.") { }

    /// <summary>Initializes a new instance of the <see cref="TypeConversionException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeConversionException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="TypeConversionException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TypeConversionException(string message, Exception? innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="TypeConversionException" /> class with conversion context.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="value">The value that could not be converted.</param>
    /// <param name="targetType">The requested target type.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, if any.</param>
    public TypeConversionException(string message, object? value, Type? targetType, Exception? innerException = null)
        : base(message, innerException)
    {
        Value = value;
        SourceType = value?.GetType();
        TargetType = targetType;
    }
}
