using Lyo.Exceptions.Models;

namespace Lyo.Exceptions;

/// <summary>Thrown when a requested resource or entity is not found. Maps to HTTP 404.</summary>
/// <remarks>
/// Prefer <see cref="ForResource" /> over the two-argument constructors: <c>new NotFoundException("User", null)</c> binds to the <c>(string message, Exception?)</c> overload
/// and never sets <see cref="ResourceName" />.
/// </remarks>
public class NotFoundException : HttpException
{
    private const int HttpStatusCode = 404;

    /// <summary>Name or identifier of the resource that was not found.</summary>
    public string? ResourceName { get; }

    /// <summary>Identifier of the resource that was not found, when applicable.</summary>
    public object? ResourceId { get; }

    /// <summary>Mints a new <see cref="NotFoundException" />.</summary>
    public NotFoundException()
        : base(HttpStatusCode, "The requested resource was not found.") { }

    /// <summary>Builds a <see cref="NotFoundException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public NotFoundException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public NotFoundException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Mints a <see cref="NotFoundException" /> with resource information.</summary>
    /// <param name="resourceName">Name or type of the resource that was not found.</param>
    /// <param name="resourceId">Identifier of the resource that was not found.</param>
    public NotFoundException(string resourceName, object? resourceId = null)
        : base(HttpStatusCode, resourceId != null ? $"{resourceName} with ID '{resourceId}' was not found." : $"{resourceName} was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Mints a <see cref="NotFoundException" /> with resource information and an inner exception.</summary>
    /// <param name="resourceName">Name or type of the resource that was not found.</param>
    /// <param name="resourceId">Identifier of the resource that was not found.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public NotFoundException(string resourceName, object? resourceId, Exception? innerException)
        : base(HttpStatusCode, resourceId != null ? $"{resourceName} with ID '{resourceId}' was not found." : $"{resourceName} was not found.", innerException)
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Builds a <see cref="NotFoundException" /> for a resource, guaranteeing <see cref="ResourceName" /> and <see cref="ResourceId" /> are set.</summary>
    /// <param name="resourceName">Name or type of the resource that was not found.</param>
    /// <param name="resourceId">Identifier of the resource that was not found.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public static NotFoundException ForResource(string resourceName, object? resourceId = null, Exception? innerException = null) => new(resourceName, resourceId, innerException);
}