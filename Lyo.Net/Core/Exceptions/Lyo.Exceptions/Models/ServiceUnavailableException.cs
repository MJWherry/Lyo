namespace Lyo.Exceptions.Models;

/// <summary>HTTP 503 when a service or resource is temporarily unavailable.</summary>
public class ServiceUnavailableException : HttpException
{
    private const int HttpStatusCode = 503;

    /// <inheritdoc />
    public override bool IsTransient => true;

    /// <summary>Name of the service that is down.</summary>
    public string? ServiceName { get; }

    /// <summary>Suggested wait before retry, when supplied.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Mints a <see cref="ServiceUnavailableException" /> with the default message.</summary>
    public ServiceUnavailableException()
        : base(HttpStatusCode, "The service is temporarily unavailable.") { }

    /// <summary>Builds a <see cref="ServiceUnavailableException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public ServiceUnavailableException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public ServiceUnavailableException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Builds a <see cref="ServiceUnavailableException" /> that names the down service.</summary>
    /// <param name="serviceName">Name of the service that is down.</param>
    /// <param name="retryAfter">Suggested wait before retry.</param>
    public ServiceUnavailableException(string serviceName, TimeSpan? retryAfter = null)
        : base(HttpStatusCode, BuildMessage(serviceName, retryAfter))
    {
        ServiceName = serviceName;
        RetryAfter = retryAfter;
    }

    /// <summary>Builds a <see cref="ServiceUnavailableException" /> that names the down service and wraps <paramref name="innerException" />.</summary>
    /// <param name="serviceName">Name of the service that is down.</param>
    /// <param name="retryAfter">Suggested wait before retry.</param>
    /// <param name="innerException">Cause of this exception.</param>
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