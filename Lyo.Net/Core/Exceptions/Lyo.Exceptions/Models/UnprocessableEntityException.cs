namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a request is well-formed but semantically invalid (e.g. failed validation). Maps to HTTP 422.</summary>
public class UnprocessableEntityException : HttpException
{
    private const int HttpStatusCode = 422;

    /// <summary>Gets the collection of field-level errors, keyed by field name. Empty when no field details were provided.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    public override string Message {
        get {
            var baseMessage = base.Message;
            if (Errors.Count > 0) {
                var errorDetails = Errors.SelectMany(kvp => kvp.Value.Select(error => $"  - {kvp.Key}: {error}"));
                return $"{baseMessage}\nErrors:\n{string.Join("\n", errorDetails)}";
            }

            return baseMessage;
        }
    }

    /// <summary>Initializes a new instance of the <see cref="UnprocessableEntityException" /> class.</summary>
    public UnprocessableEntityException()
        : base(HttpStatusCode, "The request could not be processed.")
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Initializes a new instance of the <see cref="UnprocessableEntityException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public UnprocessableEntityException(string message)
        : base(HttpStatusCode, message)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Initializes a new instance of the <see cref="UnprocessableEntityException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnprocessableEntityException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Initializes a new instance of the <see cref="UnprocessableEntityException" /> class with field-level errors.</summary>
    /// <param name="errors">A dictionary of field names to their error messages.</param>
    /// <param name="message">The message that describes the error. A default message is used when null.</param>
    public UnprocessableEntityException(IReadOnlyDictionary<string, IReadOnlyList<string>> errors, string? message = null)
        : base(HttpStatusCode, message ?? "The request could not be processed. See Errors property for details.")
        => Errors = errors != null
            ? errors.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)(kvp.Value?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>()))
            : new Dictionary<string, IReadOnlyList<string>>();
}
