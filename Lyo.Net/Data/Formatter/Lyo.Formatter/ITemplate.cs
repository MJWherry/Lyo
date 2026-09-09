namespace Lyo.Formatter;

/// <summary>Reusable template that can be validated, inspected for placeholders, and rendered against accumulated context.</summary>
public interface ITemplate
{
    /// <summary>Source template text.</summary>
    string TemplateString { get; }

    /// <summary>True when the template parses.</summary>
    bool Validate();

    /// <summary>Checks the template and returns failure text when it does not parse.</summary>
    /// <param name="errorMessage">Failure text when validation fails.</param>
    /// <returns>True when syntactically valid.</returns>
    bool TryValidate(out string? errorMessage);

    /// <summary>Placeholder names/paths in the template (e.g. "Name", "Docket.Number").</summary>
    IReadOnlyList<string> GetPlaceholders();

    /// <summary>True when accumulated context covers every placeholder. Call after AddContext/WithContext/WithValue.</summary>
    /// <param name="errorMessage">Names the placeholders that lack context when validation fails.</param>
    /// <returns>True when every placeholder has context.</returns>
    bool TryValidateContext(out string? errorMessage);

    /// <summary>Adds context via the fluent builder.</summary>
    /// <param name="configure">Builds context.</param>
    /// <returns>This template for chaining.</returns>
    ITemplate AddContext(Action<IContextBuilder> configure);

    /// <summary>Adds a context object.</summary>
    ITemplate WithContext(object? context);

    /// <summary>Adds named values from a dictionary.</summary>
    ITemplate WithContext(IReadOnlyDictionary<string, object?> context);

    /// <summary>Adds one named value to the context.</summary>
    ITemplate WithValue(string name, object? value);

    /// <summary>Renders the template against the accumulated context.</summary>
    string Format();

    /// <summary>Renders the template against the accumulated context plus extra context for this call.</summary>
    /// <param name="additionalContext">Extra context merged for this render.</param>
    /// <returns>Rendered text.</returns>
    string Format(object? additionalContext);

    /// <summary>True when no placeholder patterns remain in <paramref name="formattedOutput" />.</summary>
    /// <param name="formattedOutput">Result of Format().</param>
    /// <returns>True when every placeholder was replaced.</returns>
    bool AllPlaceholdersResolved(string formattedOutput);

    /// <summary>Placeholder names that still appear unresolved in <paramref name="formattedOutput" />.</summary>
    /// <param name="formattedOutput">Result of Format().</param>
    /// <returns>Placeholders that still appear as {Name} or {Name:...}.</returns>
    IReadOnlyList<string> GetUnresolvedPlaceholders(string formattedOutput);
}