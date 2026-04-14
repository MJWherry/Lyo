using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a conflict occurs, such as a duplicate resource or concurrent modification. Maps to HTTP 409.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ConflictException : HttpException
{
    private const int HttpStatusCode = 409;

    /// <summary>Gets the name or identifier of the resource that caused the conflict.</summary>
    public string? ResourceName { get; }

    /// <summary>Gets the identifier of the resource that caused the conflict, if applicable.</summary>
    public object? ResourceId { get; }

    /// <summary>Initializes a new instance of the <see cref="ConflictException" /> class.</summary>
    public ConflictException()
        : base(HttpStatusCode, "A conflict occurred.") { }

    /// <summary>Initializes a new instance of the <see cref="ConflictException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ConflictException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="ConflictException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConflictException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="ConflictException" /> class with resource information.</summary>
    /// <param name="resourceName">The name or type of the resource that caused the conflict.</param>
    /// <param name="resourceId">The identifier of the resource that caused the conflict.</param>
    public ConflictException(string resourceName, object? resourceId = null)
        : base(
            HttpStatusCode,
            resourceId != null ? $"{resourceName} with ID '{resourceId}' already exists or conflicts with existing data." : $"{resourceName} conflicts with existing data.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Initializes a new instance of the <see cref="ConflictException" /> class with resource information and inner exception.</summary>
    /// <param name="resourceName">The name or type of the resource that caused the conflict.</param>
    /// <param name="resourceId">The identifier of the resource that caused the conflict.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConflictException(string resourceName, object? resourceId, Exception? innerException)
        : base(
            HttpStatusCode,
            resourceId != null ? $"{resourceName} with ID '{resourceId}' already exists or conflicts with existing data." : $"{resourceName} conflicts with existing data.",
            innerException)
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    public override string ToString() => $"{base.ToString()} (Resource: {ResourceName}, ID: {ResourceId})";
}