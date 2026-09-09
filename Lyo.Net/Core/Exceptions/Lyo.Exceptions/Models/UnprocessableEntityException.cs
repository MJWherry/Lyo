namespace Lyo.Exceptions.Models;

/// <summary>HTTP 422 when the request is well-formed but semantically invalid (for example failed validation).</summary>
public class UnprocessableEntityException : HttpException
{
    private const int HttpStatusCode = 422;

    /// <summary>Field-level errors keyed by field name. Empty when no field details were supplied.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    /// <inheritdoc />
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

    /// <summary>Mints a <see cref="UnprocessableEntityException" /> with the default message and an empty error map.</summary>
    public UnprocessableEntityException()
        : base(HttpStatusCode, "The request could not be processed.")
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Builds a <see cref="UnprocessableEntityException" /> using <paramref name="message" /> and an empty error map.</summary>
    /// <param name="message">Error text.</param>
    public UnprocessableEntityException(string message)
        : base(HttpStatusCode, message)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" /> and an empty error map.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public UnprocessableEntityException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Builds a <see cref="UnprocessableEntityException" /> from field-level <paramref name="errors" />.</summary>
    /// <param name="errors">Field names mapped to their error messages.</param>
    /// <param name="message">Error text. A default is used when null.</param>
    public UnprocessableEntityException(IReadOnlyDictionary<string, IReadOnlyList<string>> errors, string? message = null)
        : base(HttpStatusCode, message ?? "The request could not be processed. See Errors property for details.")
        => Errors = errors != null
            ? errors.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)(kvp.Value?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>()))
            : new();
}