namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a service or resource is temporarily unavailable. Maps to HTTP 503.</summary>
public class ServiceUnavailableException : HttpException
{
    private const int HttpStatusCode = 503;

    /// <summary>Gets the name of the service that is unavailable.</summary>
    public string? ServiceName { get; }

    /// <summary>Gets the retry after time, if provided.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Initializes a new instance of the <see cref="ServiceUnavailableException" /> class.</summary>
    public ServiceUnavailableException()
        : base(HttpStatusCode, "The service is temporarily unavailable.") { }

    /// <summary>Initializes a new instance of the <see cref="ServiceUnavailableException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ServiceUnavailableException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="ServiceUnavailableException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ServiceUnavailableException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="ServiceUnavailableException" /> class with service information.</summary>
    /// <param name="serviceName">The name of the service that is unavailable.</param>
    /// <param name="retryAfter">The suggested retry after time.</param>
    public ServiceUnavailableException(string serviceName, TimeSpan? retryAfter = null)
        : base(HttpStatusCode, BuildMessage(serviceName, retryAfter))
    {
        ServiceName = serviceName;
        RetryAfter = retryAfter;
    }

    /// <summary>Initializes a new instance of the <see cref="ServiceUnavailableException" /> class with service information and inner exception.</summary>
    /// <param name="serviceName">The name of the service that is unavailable.</param>
    /// <param name="retryAfter">The suggested retry after time.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ServiceUnavailableException(string serviceName, TimeSpan? retryAfter, Exception? innerException)
        : base(HttpStatusCode, BuildMessage(serviceName, retryAfter), innerException)
    {
        ServiceName = serviceName;
        RetryAfter = retryAfter;
    }

    private static string BuildMessage(string serviceName, TimeSpan? retryAfter)
    {
        var message = $"The service '{serviceName}' is temporarily unavailable.";
        if (retryAfter.HasValue)
            message += $" Please retry after {retryAfter.Value.TotalSeconds} seconds.";

        return message;
    }
}