namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a resource existed but has been permanently removed. Maps to HTTP 410.</summary>
public class GoneException : HttpException
{
    private const int HttpStatusCode = 410;

    /// <summary>Gets the name or identifier of the resource that is gone.</summary>
    public string? ResourceName { get; }

    /// <summary>Gets the identifier of the resource that is gone, if applicable.</summary>
    public object? ResourceId { get; }

    /// <summary>Initializes a new instance of the <see cref="GoneException" /> class.</summary>
    public GoneException()
        : base(HttpStatusCode, "The requested resource is no longer available.") { }

    /// <summary>Initializes a new instance of the <see cref="GoneException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public GoneException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="GoneException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GoneException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    private GoneException(string message, string resourceName, object? resourceId)
        : base(HttpStatusCode, message)
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Creates a <see cref="GoneException" /> for a resource, guaranteeing <see cref="ResourceName" /> and <see cref="ResourceId" /> are set.</summary>
    /// <param name="resourceName">The name or type of the resource that is gone.</param>
    /// <param name="resourceId">The identifier of the resource that is gone.</param>
    public static GoneException ForResource(string resourceName, object? resourceId = null)
    {
        var message = resourceId != null ? $"{resourceName} with ID '{resourceId}' is no longer available." : $"{resourceName} is no longer available.";
        return new(message, resourceName, resourceId);
    }
}