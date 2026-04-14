using Lyo.Exceptions.Models;

namespace Lyo.Exceptions;

/// <summary>Exception thrown when a requested resource or entity is not found. Maps to HTTP 404.</summary>
public class NotFoundException : HttpException
{
    private const int HttpStatusCode = 404;

    /// <summary>Gets the name or identifier of the resource that was not found.</summary>
    public string? ResourceName { get; }

    /// <summary>Gets the identifier of the resource that was not found, if applicable.</summary>
    public object? ResourceId { get; }

    /// <summary>Initializes a new instance of the <see cref="NotFoundException" /> class.</summary>
    public NotFoundException()
        : base(HttpStatusCode, "The requested resource was not found.") { }

    /// <summary>Initializes a new instance of the <see cref="NotFoundException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public NotFoundException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="NotFoundException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public NotFoundException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="NotFoundException" /> class with resource information.</summary>
    /// <param name="resourceName">The name or type of the resource that was not found.</param>
    /// <param name="resourceId">The identifier of the resource that was not found.</param>
    public NotFoundException(string resourceName, object? resourceId = null)
        : base(HttpStatusCode, resourceId != null ? $"{resourceName} with ID '{resourceId}' was not found." : $"{resourceName} was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Initializes a new instance of the <see cref="NotFoundException" /> class with resource information and inner exception.</summary>
    /// <param name="resourceName">The name or type of the resource that was not found.</param>
    /// <param name="resourceId">The identifier of the resource that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public NotFoundException(string resourceName, object? resourceId, Exception? innerException)
        : base(HttpStatusCode, resourceId != null ? $"{resourceName} with ID '{resourceId}' was not found." : $"{resourceName} was not found.", innerException)
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }
}