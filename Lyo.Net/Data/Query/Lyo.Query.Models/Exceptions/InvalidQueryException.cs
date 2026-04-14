namespace Lyo.Query.Models.Exceptions;

/// <summary>Thrown when a query configuration is invalid (e.g. invalid property names, unsupported comparisons).</summary>
public class InvalidQueryException : InvalidOperationException
{
    public InvalidQueryException(string? message = null, Exception? innerException = null)
        : base(message ?? "Invalid query configuration", innerException) { }
}