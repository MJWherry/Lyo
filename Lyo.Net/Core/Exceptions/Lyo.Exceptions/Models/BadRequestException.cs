namespace Lyo.Exceptions.Models;

/// <summary>Thrown when a request is malformed or otherwise invalid. Maps to HTTP 400.</summary>
public class BadRequestException : HttpException
{
    private const int HttpStatusCode = 400;

    /// <summary>Request parameter or field that caused the error, when supplied.</summary>
    public string? ParameterName { get; }

    /// <summary>Mints a <see cref="BadRequestException" /> with the default message.</summary>
    public BadRequestException()
        : base(HttpStatusCode, "The request is invalid.") { }

    /// <summary>Builds a <see cref="BadRequestException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public BadRequestException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public BadRequestException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Builds a <see cref="BadRequestException" /> that names the offending parameter.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="parameterName">Request parameter or field that caused the error.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public BadRequestException(string message, string parameterName, Exception? innerException = null)
        : base(HttpStatusCode, message, innerException)
        => ParameterName = parameterName;
}