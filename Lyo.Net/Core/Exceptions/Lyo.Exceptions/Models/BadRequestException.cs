namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a request is malformed or otherwise invalid. Maps to HTTP 400.</summary>
public class BadRequestException : HttpException
{
    private const int HttpStatusCode = 400;

    /// <summary>Gets the name of the request parameter or field that caused the error, if provided.</summary>
    public string? ParameterName { get; }

    /// <summary>Initializes a new instance of the <see cref="BadRequestException" /> class.</summary>
    public BadRequestException()
        : base(HttpStatusCode, "The request is invalid.") { }

    /// <summary>Initializes a new instance of the <see cref="BadRequestException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public BadRequestException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="BadRequestException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BadRequestException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="BadRequestException" /> class with a message and the offending parameter name.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="parameterName">The name of the request parameter or field that caused the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BadRequestException(string message, string parameterName, Exception? innerException = null)
        : base(HttpStatusCode, message, innerException)
        => ParameterName = parameterName;
}