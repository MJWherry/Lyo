namespace Lyo.Exceptions.Models;

/// <summary>HTTP 403 when the caller lacks permission for a resource.</summary>
/// <remarks>
/// Prefer <see cref="ForResource" /> over the multi-argument constructor: <c>new ForbiddenException("Report", null)</c> binds to the <c>(string message, Exception?)</c>
/// overload and never sets <see cref="ResourceName" />.
/// </remarks>
public class ForbiddenException : HttpException
{
    private const int HttpStatusCode = 403;

    /// <summary>Name or identifier of the resource that was denied.</summary>
    public string? ResourceName { get; }

    /// <summary>Resource id that was denied, when known.</summary>
    public object? ResourceId { get; }

    /// <summary>Why access was denied, when supplied.</summary>
    public string? Reason { get; }

    /// <summary>Mints a <see cref="ForbiddenException" /> with the default message.</summary>
    public ForbiddenException()
        : base(HttpStatusCode, "Access to this resource is forbidden.") { }

    /// <summary>Builds a <see cref="ForbiddenException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public ForbiddenException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public ForbiddenException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Builds a <see cref="ForbiddenException" /> that names the denied resource.</summary>
    /// <param name="resourceName">Name or type of the resource that was denied.</param>
    /// <param name="resourceId">Id of the resource that was denied.</param>
    /// <param name="reason">Why access was denied.</param>
    public ForbiddenException(string resourceName, object? resourceId = null, string? reason = null)
        : base(HttpStatusCode, BuildMessage(resourceName, resourceId, reason))
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
        Reason = reason;
    }

    /// <summary>
    /// Builds a <see cref="ForbiddenException" /> for a resource, guaranteeing <see cref="ResourceName" />, <see cref="ResourceId" />, and
    /// <see cref="Reason" /> are set.
    /// </summary>
    /// <param name="resourceName">Name or type of the resource that was denied.</param>
    /// <param name="resourceId">Id of the resource that was denied.</param>
    /// <param name="reason">Why access was denied.</param>
    public static ForbiddenException ForResource(string resourceName, object? resourceId = null, string? reason = null) => new(resourceName, resourceId, reason);

    private static string BuildMessage(string resourceName, object? resourceId, string? reason)
    {
        var message = resourceId != null ? $"Access to {resourceName} with ID '{resourceId}' is forbidden." : $"Access to {resourceName} is forbidden.";
        if (!string.IsNullOrWhiteSpace(reason))
            message += $" Reason: {reason}";

        return message;
    }
}