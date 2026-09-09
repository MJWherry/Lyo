namespace Lyo.Exceptions.Models;

/// <summary>HTTP 410 when a resource existed but has been permanently removed.</summary>
public class GoneException : HttpException
{
    private const int HttpStatusCode = 410;

    /// <summary>Name or identifier of the resource that is gone.</summary>
    public string? ResourceName { get; }

    /// <summary>Id of the resource that is gone, when known.</summary>
    public object? ResourceId { get; }

    /// <summary>Mints a <see cref="GoneException" /> with the default message.</summary>
    public GoneException()
        : base(HttpStatusCode, "The requested resource is no longer available.") { }

    /// <summary>Builds a <see cref="GoneException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public GoneException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public GoneException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    private GoneException(string message, string resourceName, object? resourceId)
        : base(HttpStatusCode, message)
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Mints a <see cref="GoneException" /> for a resource, guaranteeing <see cref="ResourceName" /> and <see cref="ResourceId" /> are set.</summary>
    /// <param name="resourceName">Name or type of the resource that is gone.</param>
    /// <param name="resourceId">Id of the resource that is gone.</param>
    public static GoneException ForResource(string resourceName, object? resourceId = null)
    {
        var message = resourceId != null ? $"{resourceName} with ID '{resourceId}' is no longer available." : $"{resourceName} is no longer available.";
        return new(message, resourceName, resourceId);
    }
}