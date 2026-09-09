namespace Lyo.Exceptions.Models;

/// <summary>Validation failed; often carries more than one field error.</summary>
public class ValidationException : Exception
{
    /// <summary>Field names mapped to their validation messages.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    /// <inheritdoc />
    public override string Message {
        get {
            var baseMessage = base.Message;
            if (Errors.Count > 0) {
                var errorDetails = Errors.SelectMany(kvp => kvp.Value.Select(error => $"  - {kvp.Key}: {error}"));
                return $"{baseMessage}\nValidation errors:\n{string.Join("\n", errorDetails)}";
            }

            return baseMessage;
        }
    }

    /// <summary>Mints a <see cref="ValidationException" /> with the default message and an empty error map.</summary>
    public ValidationException()
        : base("Validation failed.")
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Builds a <see cref="ValidationException" /> using <paramref name="message" /> and an empty error map.</summary>
    /// <param name="message">Error text.</param>
    public ValidationException(string message)
        : base(message)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Builds a <see cref="ValidationException" /> from <paramref name="errors" />.</summary>
    /// <param name="errors">Field names mapped to their validation messages.</param>
    public ValidationException(Dictionary<string, IReadOnlyList<string>> errors)
        : base("Validation failed. See Errors property for details.")
        => Errors = errors != null
            ? new(errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>().ToList().AsReadOnly()))
            : new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Builds a <see cref="ValidationException" /> from <paramref name="errors" /> and <paramref name="message" />.</summary>
    /// <param name="errors">Field names mapped to their validation messages.</param>
    /// <param name="message">Error text.</param>
    public ValidationException(Dictionary<string, IReadOnlyList<string>> errors, string message)
        : base(message)
        => Errors = errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)new string[] { }.ToList().AsReadOnly());

    /// <summary>Builds a <see cref="ValidationException" /> for one field failure.</summary>
    /// <param name="fieldName">Field that failed validation.</param>
    /// <param name="errorMessage">Validation message for that field.</param>
    public ValidationException(string fieldName, string errorMessage)
        : base($"Validation failed for field '{fieldName}': {errorMessage}")
        => Errors = new Dictionary<string, IReadOnlyList<string>> { { fieldName, new[] { errorMessage }.ToList().AsReadOnly() } };

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" /> and an empty error map.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public ValidationException(string message, Exception? innerException)
        : base(message, innerException)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();
}