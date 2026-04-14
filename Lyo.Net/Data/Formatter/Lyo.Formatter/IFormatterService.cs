using System.Globalization;
using SmartFormat;

namespace Lyo.Formatter;

/// <summary>Service for formatting text templates using SmartFormat with named placeholders, pluralization, localization, and rich formatting.</summary>
public interface IFormatterService
{
    /// <summary>
    /// Gets the underlying SmartFormat SmartFormatter for advanced configuration (extensions, culture, etc.). Cast to SmartFormat.Core.Extensions.SmartFormatter when
    /// configuring.
    /// </summary>
    SmartFormatter Formatter { get; }

    /// <summary>Gets or sets the culture used for formatting. Null uses the current thread culture.</summary>
    CultureInfo? Culture { get; set; }

    /// <summary>Formats a template string with the given context object(s). SmartFormat merges multiple context sources for placeholder resolution.</summary>
    /// <param name="template">The template string with placeholders (e.g. "{Name}", "{Items:list:{}|, }").</param>
    /// <param name="context">The primary context object. Can be an anonymous object, DTO, or dictionary.</param>
    /// <returns>The formatted string.</returns>
    string Format(string template, object? context);

    /// <summary>Formats a template string with multiple context objects. SmartFormat merges all sources for placeholder resolution.</summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="contextItems">One or more context objects (e.g. user, settings, globals). Later items override earlier ones for same property names.</param>
    /// <returns>The formatted string.</returns>
    string Format(string template, params object?[] contextItems);

    /// <summary>Formats a template string with a dictionary of named context values.</summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="context">Dictionary of name-to-value mappings for placeholders.</param>
    /// <returns>The formatted string.</returns>
    string Format(string template, IReadOnlyDictionary<string, object?> context);

    /// <summary>Formats a template string with context built via a fluent builder. Supports custom formatting for DateTime, numbers, etc.</summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="configure">Action that builds the context, e.g. ctx => ctx.Add("Name", name).Add("Date", dt, "yyyy-MM-dd")</param>
    /// <returns>The formatted string.</returns>
    string Format(string template, Action<IContextBuilder> configure);

    /// <summary>Attempts to format a template. Returns false if formatting fails (e.g. missing placeholder).</summary>
    /// <param name="template">The template string.</param>
    /// <param name="context">The context object.</param>
    /// <param name="result">The formatted string when successful.</param>
    /// <returns>True if formatting succeeded; false otherwise.</returns>
    bool TryFormat(string template, object? context, out string? result);

    /// <summary>Validates that a template string can be parsed. Returns true if valid; false if the template has syntax errors.</summary>
    /// <param name="template">The template string to validate.</param>
    /// <returns>True if the template is valid; false otherwise.</returns>
    bool ValidateTemplate(string template);

    /// <summary>Validates a template and returns validation result with optional error message.</summary>
    /// <param name="template">The template string to validate.</param>
    /// <param name="errorMessage">The error message when validation fails.</param>
    /// <returns>True if valid; false otherwise.</returns>
    bool TryValidateTemplate(string template, out string? errorMessage);

    /// <summary>Extracts placeholder names from a template (e.g. "Name", "Docket.Number", "Items" from "{Name}", "{Docket.Number}", "{Items:list:...}").</summary>
    /// <param name="template">The template string.</param>
    /// <returns>Distinct placeholder paths found in the template.</returns>
    IReadOnlyList<string> GetPlaceholders(string template);

    /// <summary>Creates a template that can be validated, inspected for placeholders, and formatted with context.</summary>
    /// <param name="template">The template string.</param>
    /// <returns>A template for validation, placeholder discovery, and formatting.</returns>
    ITemplate CreateTemplate(string template);

    /// <summary>Checks if any template placeholders remain unresolved in the formatted output. Returns true when all placeholders were replaced.</summary>
    /// <param name="template">The template string.</param>
    /// <param name="formattedOutput">The result of formatting the template.</param>
    /// <returns>True when no placeholder patterns remain in the output; false when one or more appear unresolved.</returns>
    bool AllPlaceholdersResolved(string template, string formattedOutput);

    /// <summary>Gets placeholder names that still appear in the formatted output (unresolved).</summary>
    /// <param name="template">The template string.</param>
    /// <param name="formattedOutput">The result of formatting the template.</param>
    /// <returns>Placeholders that still appear as {Name} or {Name:...} in the output.</returns>
    IReadOnlyList<string> GetUnresolvedPlaceholders(string template, string formattedOutput);
}