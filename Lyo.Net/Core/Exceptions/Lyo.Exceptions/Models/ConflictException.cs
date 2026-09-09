using System.Diagnostics;

namespace Lyo.Exceptions.Models;

/// <summary>HTTP 409 for a conflict such as a duplicate resource or concurrent modification.</summary>
/// <remarks>
/// Prefer <see cref="ForResource" /> over the two-argument constructors: <c>new ConflictException("Order", null)</c> binds to the <c>(string message, Exception?)</c>
/// overload and never sets <see cref="ResourceName" />.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public class ConflictException : HttpException
{
    private const int HttpStatusCode = 409;

    /// <summary>Name or identifier of the resource that conflicted.</summary>
    public string? ResourceName { get; }

    /// <summary>Id of the resource that conflicted, when known.</summary>
    public object? ResourceId { get; }

    /// <summary>Mints a <see cref="ConflictException" /> with the default message.</summary>
    public ConflictException()
        : base(HttpStatusCode, "A conflict occurred.") { }

    /// <summary>Builds a <see cref="ConflictException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public ConflictException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public ConflictException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Builds a <see cref="ConflictException" /> that names the conflicting resource.</summary>
    /// <param name="resourceName">Name or type of the resource that conflicted.</param>
    /// <param name="resourceId">Id of the resource that conflicted.</param>
    public ConflictException(string resourceName, object? resourceId = null)
        : base(
            HttpStatusCode,
            resourceId != null ? $"{resourceName} with ID '{resourceId}' already exists or conflicts with existing data." : $"{resourceName} conflicts with existing data.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Builds a <see cref="ConflictException" /> that names the conflicting resource and wraps <paramref name="innerException" />.</summary>
    /// <param name="resourceName">Name or type of the resource that conflicted.</param>
    /// <param name="resourceId">Id of the resource that conflicted.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public ConflictException(string resourceName, object? resourceId, Exception? innerException)
        : base(
            HttpStatusCode,
            resourceId != null ? $"{resourceName} with ID '{resourceId}' already exists or conflicts with existing data." : $"{resourceName} conflicts with existing data.",
            innerException)
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Mints a <see cref="ConflictException" /> for a resource, guaranteeing <see cref="ResourceName" /> and <see cref="ResourceId" /> are set.</summary>
    /// <param name="resourceName">Name or type of the resource that conflicted.</param>
    /// <param name="resourceId">Id of the resource that conflicted.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public static ConflictException ForResource(string resourceName, object? resourceId = null, Exception? innerException = null) => new(resourceName, resourceId, innerException);

    /// <inheritdoc />
    public override string ToString() => $"{base.ToString()} (Resource: {ResourceName}, ID: {ResourceId})";
}