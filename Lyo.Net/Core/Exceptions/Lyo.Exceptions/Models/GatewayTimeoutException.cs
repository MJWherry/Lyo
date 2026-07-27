namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when an upstream service did not respond in time. Maps to HTTP 504.</summary>
public class GatewayTimeoutException : HttpException
{
    private const int HttpStatusCode = 504;

    /// <inheritdoc />
    public override bool IsTransient => true;

    /// <summary>Gets the name of the upstream service that timed out, if provided.</summary>
    public string? ServiceName { get; }

    /// <summary>Gets the timeout that was exceeded, if provided.</summary>
    public TimeSpan? Timeout { get; }

    /// <summary>Initializes a new instance of the <see cref="GatewayTimeoutException" /> class.</summary>
    public GatewayTimeoutException()
        : base(HttpStatusCode, "The upstream service did not respond in time.") { }

    /// <summary>Initializes a new instance of the <see cref="GatewayTimeoutException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public GatewayTimeoutException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="GatewayTimeoutException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GatewayTimeoutException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="GatewayTimeoutException" /> class with service information.</summary>
    /// <param name="serviceName">The name of the upstream service that timed out.</param>
    /// <param name="timeout">The timeout that was exceeded.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GatewayTimeoutException(string serviceName, TimeSpan? timeout, Exception? innerException = null)
        : base(HttpStatusCode, BuildMessage(serviceName, timeout), innerException)
    {
        ServiceName = serviceName;
        Timeout = timeout;
    }

    private static string BuildMessage(string serviceName, TimeSpan? timeout)
    {
        var message = $"The upstream service '{serviceName}' did not respond in time.";
        if (timeout.HasValue)
            message += $" Timeout: {timeout.Value.TotalSeconds} seconds.";

        return message;
    }
}
