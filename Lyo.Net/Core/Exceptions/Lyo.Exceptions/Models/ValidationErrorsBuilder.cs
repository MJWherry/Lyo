namespace Lyo.Exceptions.Models;

/// <summary>
/// Accumulates field-level validation errors and throws a single <see cref="ValidationException" /> only when errors exist. Avoids hand-building the errors dictionary and
/// throwing on the first failure.
/// </summary>
/// <example>
/// <code>
/// var builder = new ValidationErrorsBuilder();
/// if (string.IsNullOrWhiteSpace(request.Email))
///     builder.Add(nameof(request.Email), "Email is required.");
/// if (request.Age &lt; 0)
///     builder.Add(nameof(request.Age), "Age must be positive.");
/// builder.ThrowIfAny();
/// </code>
/// </example>
public sealed class ValidationErrorsBuilder
{
    private readonly Dictionary<string, List<string>> _errors = new();

    /// <summary>Gets whether any errors have been added.</summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>Gets the total number of error messages across all fields.</summary>
    public int Count => _errors.Values.Sum(messages => messages.Count);

    /// <summary>Adds an error message for a field. Multiple messages may be added for the same field.</summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName" /> or <paramref name="errorMessage" /> is null, empty, or whitespace.</exception>
    public ValidationErrorsBuilder Add(string fieldName, string errorMessage)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorMessage);
        if (!_errors.TryGetValue(fieldName, out var messages)) {
            messages = new();
            _errors[fieldName] = messages;
        }

        messages.Add(errorMessage);
        return this;
    }

    /// <summary>Adds multiple error messages for a field. Null, empty, and whitespace-only messages are skipped.</summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="errorMessages">The validation error messages.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName" /> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorMessages" /> is null.</exception>
    public ValidationErrorsBuilder AddRange(string fieldName, IEnumerable<string> errorMessages)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentHelpers.ThrowIfNull(errorMessages);
        foreach (var message in errorMessages.Where(m => !string.IsNullOrWhiteSpace(m)))
            Add(fieldName, message);

        return this;
    }

    /// <summary>Adds an error message for a field only when <paramref name="condition" /> is true.</summary>
    /// <param name="condition">When true, the error is added.</param>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>This builder, for chaining.</returns>
    public ValidationErrorsBuilder AddIf(bool condition, string fieldName, string errorMessage) => condition ? Add(fieldName, errorMessage) : this;

    /// <summary>Returns the accumulated errors as the dictionary shape used by <see cref="ValidationException.Errors" />.</summary>
    public Dictionary<string, IReadOnlyList<string>> Build() => _errors.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());

    /// <summary>Throws a <see cref="ValidationException" /> containing all accumulated errors when any exist; otherwise does nothing.</summary>
    /// <param name="message">The exception message. A default message is used when null.</param>
    /// <exception cref="ValidationException">Thrown when any errors have been added.</exception>
    public void ThrowIfAny(string? message = null)
    {
        if (!HasErrors)
            return;

        throw message != null ? new ValidationException(Build(), message) : new ValidationException(Build());
    }
}