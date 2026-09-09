using System.Globalization;
using SmartFormat;

namespace Lyo.Formatter;

/// <summary>Formats SmartFormat templates with named placeholders and C#-like expressions (DateTime, ternary, in-memory LINQ).</summary>
public interface IFormatterService
{
    /// <summary>
    /// Underlying SmartFormat <see cref="SmartFormatter" /> for extensions, culture, and similar setup. Cast to SmartFormat.Core.Extensions.SmartFormatter when configuring.
    /// </summary>
    SmartFormatter Formatter { get; }

    /// <summary>Culture used for formatting. Null uses the current thread culture.</summary>
    CultureInfo? Culture { get; set; }

    /// <summary>Renders <paramref name="template" /> against <paramref name="context" />. SmartFormat merges multiple sources when resolving placeholders.</summary>
    /// <param name="template">Template with placeholders (e.g. "{Name}", "{Items:list:{}|, }").</param>
    /// <param name="context">Primary context: anonymous object, DTO, or dictionary.</param>
    /// <returns>Rendered text.</returns>
    string Format(string template, object? context);

    /// <summary>Renders <paramref name="template" /> against several context objects. Later items win on colliding property names.</summary>
    /// <param name="template">Template with placeholders.</param>
    /// <param name="contextItems">Context objects (e.g. user, settings, globals).</param>
    /// <returns>Rendered text.</returns>
    string Format(string template, params object?[] contextItems);

    /// <summary>Renders <paramref name="template" /> against a name-to-value dictionary.</summary>
    /// <param name="template">Template with placeholders.</param>
    /// <param name="context">Placeholder name → value map.</param>
    /// <returns>Rendered text.</returns>
    string Format(string template, IReadOnlyDictionary<string, object?> context);

    /// <summary>Renders <paramref name="template" /> using a fluent context builder (DateTime/number format strings, custom formatters).</summary>
    /// <param name="template">Template with placeholders.</param>
    /// <param name="configure">Builds context, e.g. ctx => ctx.Add("Name", name).Add("Date", dt, "yyyy-MM-dd")</param>
    /// <returns>Rendered text.</returns>
    string Format(string template, Action<IContextBuilder> configure);

    /// <summary>Tries to render a template. Returns false when formatting fails (e.g. a missing placeholder).</summary>
    /// <param name="template">Template text.</param>
    /// <param name="context">Context object.</param>
    /// <param name="result">Rendered text on success.</param>
    /// <returns>True when formatting succeeded.</returns>
    bool TryFormat(string template, object? context, out string? result);

    /// <summary>True when <paramref name="template" /> parses; false when it has syntax errors.</summary>
    /// <param name="template">Template to check.</param>
    /// <returns>True when the template is syntactically valid.</returns>
    bool ValidateTemplate(string template);

    /// <summary>Checks template syntax. Missing context names are not errors (use <see cref="ITemplate.TryValidateContext" /> for coverage).</summary>
    /// <param name="template">Template to check.</param>
    /// <param name="errorMessage">Failure text when syntax is invalid.</param>
    /// <returns>True when syntactically valid.</returns>
    bool TryValidateTemplate(string template, out string? errorMessage);

    /// <summary>Distinct placeholder paths in a template (e.g. "Name", "Docket.Number", "amount" from "{Name}", "{Docket.Number}", "{this.amount > 2 ? \"x\" : \"y\"}").</summary>
    /// <param name="template">Template text.</param>
    /// <returns>Distinct placeholder paths.</returns>
    IReadOnlyList<string> GetPlaceholders(string template);

    /// <summary>Builds a reusable template for validation, placeholder discovery, and formatting.</summary>
    /// <param name="template">Template text.</param>
    /// <returns>Template handle.</returns>
    ITemplate CreateTemplate(string template);

    /// <summary>True when no placeholder patterns remain in <paramref name="formattedOutput" />.</summary>
    /// <param name="template">Original template.</param>
    /// <param name="formattedOutput">Result of formatting the template.</param>
    /// <returns>True when every placeholder was replaced.</returns>
    bool AllPlaceholdersResolved(string template, string formattedOutput);

    /// <summary>Placeholder names that still appear in <paramref name="formattedOutput" />.</summary>
    /// <param name="template">Original template.</param>
    /// <param name="formattedOutput">Result of formatting the template.</param>
    /// <returns>Placeholders that still appear as {Name} or {Name:...}.</returns>
    IReadOnlyList<string> GetUnresolvedPlaceholders(string template, string formattedOutput);

    /// <summary>
    /// Renders a template as ordered spans (literals and per-placeholder replacements) so UIs can color-link keys to values. Empty templates yield an empty list; invalid
    /// syntax yields one literal span of the original text. Nested or list placeholders are one outer span keyed by the selector path (e.g. <c>Items</c>).
    /// </summary>
    /// <param name="template">Template with placeholders.</param>
    /// <param name="context">Primary context: anonymous object, DTO, dictionary, or null.</param>
    /// <returns>Spans covering the whole template in order.</returns>
    IReadOnlyList<FormatterSegment> FormatSegments(string template, object? context);

    /// <summary>Renders a template as ordered spans against a name-to-value dictionary.</summary>
    /// <param name="template">Template with placeholders.</param>
    /// <param name="context">Placeholder name → value map.</param>
    /// <returns>Spans covering the whole template in order.</returns>
    IReadOnlyList<FormatterSegment> FormatSegments(string template, IReadOnlyDictionary<string, object?> context);
}