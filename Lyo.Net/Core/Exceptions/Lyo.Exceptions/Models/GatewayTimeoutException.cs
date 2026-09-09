namespace Lyo.Exceptions.Models;

/// <summary>HTTP 504 when an upstream service did not respond in time.</summary>
public class GatewayTimeoutException : HttpException
{
    private const int HttpStatusCode = 504;

    /// <inheritdoc />
    public override bool IsTransient => true;

    /// <summary>Name of the upstream service that timed out, when supplied.</summary>
    public string? ServiceName { get; }

    /// <summary>Timeout that was exceeded, when supplied.</summary>
    public TimeSpan? Timeout { get; }

    /// <summary>Mints a <see cref="GatewayTimeoutException" /> with the default message.</summary>
    public GatewayTimeoutException()
        : base(HttpStatusCode, "The upstream service did not respond in time.") { }

    /// <summary>Builds a <see cref="GatewayTimeoutException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public GatewayTimeoutException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public GatewayTimeoutException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Builds a <see cref="GatewayTimeoutException" /> that names the timed-out service.</summary>
    /// <param name="serviceName">Name of the upstream service that timed out.</param>
    /// <param name="timeout">Timeout that was exceeded.</param>
    /// <param name="innerException">Cause of this exception.</param>
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