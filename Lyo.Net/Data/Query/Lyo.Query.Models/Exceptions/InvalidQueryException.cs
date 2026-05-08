namespace Lyo.Query.Models.Exceptions;

/// <summary>Thrown when a query configuration is invalid (e.g. invalid property names, unsupported comparisons).</summary>
/// <param name="message">The error message, or null for a default message.</param>
/// <param name="innerException">The underlying exception, if any.</param>
public class InvalidQueryException(string? message = null, Exception? innerException = null)
    : InvalidOperationException(message ?? "Invalid query configuration", innerException);