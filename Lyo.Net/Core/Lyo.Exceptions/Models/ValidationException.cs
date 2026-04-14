namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when validation fails, typically containing multiple validation errors.</summary>
public class ValidationException : Exception
{
    /// <summary>Gets the collection of validation errors.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

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

    /// <summary>Initializes a new instance of the <see cref="ValidationException" /> class.</summary>
    public ValidationException()
        : base("Validation failed.")
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Initializes a new instance of the <see cref="ValidationException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ValidationException(string message)
        : base(message)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Initializes a new instance of the <see cref="ValidationException" /> class with validation errors.</summary>
    /// <param name="errors">A dictionary of field names to their validation error messages.</param>
    public ValidationException(Dictionary<string, IReadOnlyList<string>> errors)
        : base("Validation failed. See Errors property for details.")
        => Errors = errors != null
            ? new(errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>().ToList().AsReadOnly()))
            : new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Initializes a new instance of the <see cref="ValidationException" /> class with validation errors.</summary>
    /// <param name="errors">A dictionary of field names to their validation error messages.</param>
    /// <param name="message">The message that describes the error.</param>
    public ValidationException(Dictionary<string, IReadOnlyList<string>> errors, string message)
        : base(message)
        => Errors = errors != null
            ? new(errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)new string[] { }.ToList().AsReadOnly()))
            : new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Initializes a new instance of the <see cref="ValidationException" /> class with a single validation error.</summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
    public ValidationException(string fieldName, string errorMessage)
        : base($"Validation failed for field '{fieldName}': {errorMessage}")
        => Errors = new Dictionary<string, IReadOnlyList<string>> { { fieldName, new[] { errorMessage }.ToList().AsReadOnly() } };

    /// <summary>Initializes a new instance of the <see cref="ValidationException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ValidationException(string message, Exception? innerException)
        : base(message, innerException)
        => Errors = new Dictionary<string, IReadOnlyList<string>>();
}