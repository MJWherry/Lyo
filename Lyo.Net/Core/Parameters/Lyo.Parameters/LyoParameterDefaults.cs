using Lyo.Exceptions;

namespace Lyo.Parameters;

/// <summary>
/// Renders a parameter default template to text. Hosts that reference <c>Lyo.Formatter</c> implement this over <c>IFormatterService</c>; declared here so Core stays free of the
/// formatter dependency.
/// </summary>
/// <param name="template">Template as declared on the parameter, for example <c>{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}</c>.</param>
/// <returns>Rendered text, or null when the template produced nothing.</returns>
public delegate string? LyoTemplateResolver(string template);

/// <summary>
/// Resolves the default a declared parameter contributes when a caller supplies no value. Expression defaults are rendered and normalized to the declared type here, before
/// <see cref="LyoParameterValidator" /> sees them, so validation stays type-strict no matter how the default was authored.
/// </summary>
/// <remarks>
/// Register a <see cref="LyoTemplateResolver" /> in DI for a host whose definitions use expression defaults. Hosts without one keep working: only a parameter that actually
/// declares an expression default fails, and it fails with a message naming the missing registration rather than silently yielding no value.
/// </remarks>
public static class LyoParameterDefaults
{
    /// <summary>True when the parameter's default is a template that must be rendered before use.</summary>
    /// <param name="spec">Parameter as declared.</param>
    public static bool IsExpression(LyoParameterSpec spec)
    {
        ArgumentHelpers.ThrowIfNull(spec);
        return spec.DefaultKind == LyoParameterDefaultKind.Expression && !string.IsNullOrWhiteSpace(spec.DefaultTemplate);
    }

    /// <summary>
    /// True when the parameter contributes anything if the caller omits it: a literal default, an expression default, or ciphertext. Callers back-filling a run or generation
    /// snapshot use this to decide which declared parameters to carry over.
    /// </summary>
    /// <param name="spec">Parameter as declared.</param>
    /// <param name="literalValue">The definition's literal default, typically its stored <c>Value</c>.</param>
    /// <param name="hasEncryptedValue">True when the definition carries ciphertext, which is a value in its own right.</param>
    public static bool HasDefault(LyoParameterSpec spec, string? literalValue, bool hasEncryptedValue = false)
    {
        ArgumentHelpers.ThrowIfNull(spec);
        return hasEncryptedValue || IsExpression(spec) || !string.IsNullOrWhiteSpace(literalValue);
    }

    /// <summary>
    /// Resolves the parameter's default. Literal defaults pass through unchanged; expression defaults are rendered through <paramref name="resolver" /> and normalized to the
    /// declared type, so <c>{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}</c> on a <c>System.DateTime</c> parameter becomes a value the validator accepts.
    /// </summary>
    /// <param name="spec">Parameter as declared.</param>
    /// <param name="literalValue">The definition's literal default, returned as-is unless the spec declares an expression default.</param>
    /// <param name="resolver">Template renderer, typically resolved from DI. Null is only a problem when the spec declares an expression default.</param>
    /// <param name="value">Resolved value, ready to validate and store.</param>
    /// <param name="error">Why resolution failed, phrased for a caller-facing validation list. Null when this returns true.</param>
    /// <returns>True when <paramref name="value" /> can be used, including when the spec has no default at all.</returns>
    public static bool TryResolve(LyoParameterSpec spec, string? literalValue, LyoTemplateResolver? resolver, out string? value, out string? error)
    {
        ArgumentHelpers.ThrowIfNull(spec);
        error = null;

        if (!IsExpression(spec)) {
            value = literalValue;
            return true;
        }

        value = null;
        if (resolver is null) {
            error = $"Parameter '{spec.Key}' has an expression default but no template resolver is registered on this host.";
            return false;
        }

        string? rendered;
        try {
            rendered = resolver(spec.DefaultTemplate!);
        }
        catch (Exception ex) {
            error = $"Parameter '{spec.Key}' default expression could not be rendered: {ex.Message}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(rendered)) {
            error = $"Parameter '{spec.Key}' default expression rendered an empty value.";
            return false;
        }

        var normalized = LyoParameterValueJson.Normalize(spec.Type, rendered);
        if (!LyoParameterValueJson.IsAssignable(spec.Type, normalized)) {
            error = $"Parameter '{spec.Key}' default expression rendered '{rendered}', which is not a valid {spec.Type}.";
            return false;
        }

        value = normalized;
        return true;
    }
}
