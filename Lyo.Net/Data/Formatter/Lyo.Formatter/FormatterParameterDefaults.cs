using Lyo.Exceptions;
using Lyo.Parameters;

namespace Lyo.Formatter;

/// <summary>
/// Connects SmartFormat to <see cref="LyoParameterDefaults" />: renders a parameter's <see cref="ILyoParameterDefinition.DefaultTemplate" /> and checks the result against the
/// parameter's declared type. A <c>System.DateTime</c> parameter defaulting to <c>{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}</c> is validated here as a date, not as a template.
/// </summary>
/// <remarks>
/// <see cref="CreateResolver" /> produces the <see cref="LyoTemplateResolver" /> that Core-side resolution needs; <c>AddFormatterService</c> registers a context-free one, so any
/// host with the formatter resolves expression defaults without extra wiring.
/// </remarks>
public static class FormatterParameterDefaults
{
    /// <summary>Adapts a formatter into the template resolver <see cref="LyoParameterDefaults.TryResolve" /> expects. Render failures throw so the caller can report them.</summary>
    /// <param name="formatter">Formatter doing the rendering.</param>
    /// <param name="context">Context object or dictionary placeholders resolve against. Null suits self-contained templates such as <c>{DateTime.UtcNow}</c>.</param>
    public static LyoTemplateResolver CreateResolver(IFormatterService formatter, object? context = null)
    {
        ArgumentHelpers.ThrowIfNull(formatter);
        return template => formatter.Format(template, context);
    }

    /// <summary>
    /// Checks a default template end to end: syntax, that it renders, and that the result satisfies <paramref name="type" />. Use from definition write validators and from the
    /// editor's live preview so the message an author sees is the one that would have blocked the save.
    /// </summary>
    /// <param name="formatter">Formatter doing the rendering.</param>
    /// <param name="type">Declared parameter type the rendered value must satisfy.</param>
    /// <param name="template">Template as authored.</param>
    /// <param name="rendered">Rendered text, suitable for showing in a preview. Null when rendering failed.</param>
    /// <param name="error">Why the template is unusable, phrased for the author. Null when this returns true.</param>
    /// <param name="context">Context object or dictionary placeholders resolve against.</param>
    /// <returns>True when the template renders to a value of the declared type.</returns>
    public static bool TryValidate(
        IFormatterService formatter,
        string? type,
        string? template,
        out string? rendered,
        out string? error,
        object? context = null)
    {
        ArgumentHelpers.ThrowIfNull(formatter);
        rendered = null;

        if (string.IsNullOrWhiteSpace(template)) {
            error = "Default template is required.";
            return false;
        }

        if (!formatter.TryValidateTemplate(template!, out var syntaxError)) {
            error = syntaxError ?? "Default template is not valid.";
            return false;
        }

        try {
            rendered = formatter.Format(template!, context);
        }
        catch (Exception ex) {
            error = $"Default template could not be rendered: {ex.Message}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(rendered)) {
            error = "Default template renders an empty value.";
            return false;
        }

        if (!LyoParameterValueJson.IsAssignable(type, LyoParameterValueJson.Normalize(type, rendered))) {
            error = $"Default template renders '{rendered}', which is not a valid {type}.";
            return false;
        }

        error = null;
        return true;
    }
}
