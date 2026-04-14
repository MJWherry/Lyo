namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when access to a resource is forbidden due to insufficient permissions. Maps to HTTP 403.</summary>
public class ForbiddenException : HttpException
{
    private const int HttpStatusCode = 403;

    /// <summary>Gets the name or identifier of the resource that access was forbidden for.</summary>
    public string? ResourceName { get; }

    /// <summary>Gets the identifier of the resource that access was forbidden for, if applicable.</summary>
    public object? ResourceId { get; }

    /// <summary>Gets the reason for the forbidden access, if provided.</summary>
    public string? Reason { get; }

    /// <summary>Initializes a new instance of the <see cref="ForbiddenException" /> class.</summary>
    public ForbiddenException()
        : base(HttpStatusCode, "Access to this resource is forbidden.") { }

    /// <summary>Initializes a new instance of the <see cref="ForbiddenException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ForbiddenException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="ForbiddenException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ForbiddenException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="ForbiddenException" /> class with resource information.</summary>
    /// <param name="resourceName">The name or type of the resource that access was forbidden for.</param>
    /// <param name="resourceId">The identifier of the resource that access was forbidden for.</param>
    /// <param name="reason">The reason for the forbidden access.</param>
    public ForbiddenException(string resourceName, object? resourceId = null, string? reason = null)
        : base(HttpStatusCode, BuildMessage(resourceName, resourceId, reason))
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
        Reason = reason;
    }

    private static string BuildMessage(string resourceName, object? resourceId, string? reason)
    {
        var message = resourceId != null ? $"Access to {resourceName} with ID '{resourceId}' is forbidden." : $"Access to {resourceName} is forbidden.";
        if (!string.IsNullOrWhiteSpace(reason))
            message += $" Reason: {reason}";

        return message;
    }
}