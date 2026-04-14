namespace Lyo.Formatter;

/// <summary>Represents a template that can be validated, inspected for placeholders, and formatted with context.</summary>
public interface ITemplate
{
    /// <summary>Gets the template string.</summary>
    string TemplateString { get; }

    /// <summary>Validates the template. Returns true if the template can be parsed.</summary>
    bool Validate();

    /// <summary>Validates the template and returns the error message when validation fails.</summary>
    /// <param name="errorMessage">The error message when validation fails.</param>
    /// <returns>True if valid; false otherwise.</returns>
    bool TryValidate(out string? errorMessage);

    /// <summary>Gets the placeholder names/paths in the template (e.g. "Name", "Docket.Number").</summary>
    IReadOnlyList<string> GetPlaceholders();

    /// <summary>Validates that the accumulated context satisfies all placeholders. Use after AddContext/WithContext/WithValue. Returns false when placeholders are missing from context.</summary>
    /// <param name="errorMessage">Describes which placeholders are missing context when validation fails.</param>
    /// <returns>True when all placeholders have corresponding context; false otherwise.</returns>
    bool TryValidateContext(out string? errorMessage);

    /// <summary>Adds context using the fluent builder.</summary>
    /// <param name="configure">Action that builds the context.</param>
    /// <returns>This template for chaining.</returns>
    ITemplate AddContext(Action<IContextBuilder> configure);

    /// <summary>Adds a context object.</summary>
    ITemplate WithContext(object? context);

    /// <summary>Adds named context from a dictionary.</summary>
    ITemplate WithContext(IReadOnlyDictionary<string, object?> context);

    /// <summary>Adds a named value to the context.</summary>
    ITemplate WithValue(string name, object? value);

    /// <summary>Formats the template with the accumulated context.</summary>
    string Format();

    /// <summary>Formats the template with the accumulated context plus additional context for this call.</summary>
    /// <param name="additionalContext">Additional context merged for this format call.</param>
    /// <returns>The formatted string.</returns>
    string Format(object? additionalContext);

    /// <summary>Checks if all placeholders were properly replaced in the formatted output. Returns true when no placeholder patterns remain.</summary>
    /// <param name="formattedOutput">The result of Format().</param>
    /// <returns>True when all placeholders were resolved; false when one or more appear unresolved in the output.</returns>
    bool AllPlaceholdersResolved(string formattedOutput);

    /// <summary>Gets placeholder names that still appear unresolved in the formatted output.</summary>
    /// <param name="formattedOutput">The result of Format().</param>
    /// <returns>Placeholders that still appear as {Name} or {Name:...} in the output.</returns>
    IReadOnlyList<string> GetUnresolvedPlaceholders(string formattedOutput);
}