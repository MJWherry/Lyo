namespace Lyo.Formatter;

/// <summary>Fluent helper that attaches named context values (and optional formatters) to a template.</summary>
public interface IContextBuilder
{
    /// <summary>Adds a raw value. Use for strings, numbers, or objects that SmartFormat can format from the template.</summary>
    /// <param name="key">Placeholder name (e.g. "UserName" for "{UserName}").</param>
    /// <param name="value">Value to bind.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add(string key, object? value);

    /// <summary>Adds a value with an IFormattable format string (e.g. "yyyy-MM-dd", "N2", "C").</summary>
    /// <param name="key">Placeholder name.</param>
    /// <param name="value">Value to format.</param>
    /// <param name="format">Format string (e.g. "yyyy-MM-dd" for DateTime, "N2" for decimals, "C" for currency).</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add(string key, object? value, string format);

    /// <summary>Adds a value with a custom formatter that owns the output string.</summary>
    /// <param name="key">Placeholder name.</param>
    /// <param name="value">Value to bind.</param>
    /// <param name="formatter">Converts the value to a string. Return null for empty.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add(string key, object? value, Func<object?, string?> formatter);

    /// <summary>Adds a typed value with a custom formatter.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Placeholder name.</param>
    /// <param name="value">Value to bind.</param>
    /// <param name="formatter">Converts the value to a string.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add<T>(string key, T? value, Func<T?, string?> formatter);

    /// <summary>Adds a value only when <paramref name="condition" /> is true.</summary>
    /// <param name="key">Placeholder name.</param>
    /// <param name="value">Value to bind.</param>
    /// <param name="condition">When true the value is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddIf(string key, object? value, bool condition);

    /// <summary>Adds a value only when <paramref name="condition" /> is true, using an IFormattable format string.</summary>
    /// <param name="key">Placeholder name.</param>
    /// <param name="value">Value to bind.</param>
    /// <param name="format">Format string (e.g. "yyyy-MM-dd", "N2").</param>
    /// <param name="condition">When true the value is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddIf(string key, object? value, string format, bool condition);

    /// <summary>Adds a value when <paramref name="predicate" /> returns true for that value.</summary>
    /// <param name="key">Placeholder name.</param>
    /// <param name="value">Value to bind.</param>
    /// <param name="predicate">When true for the value, it is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddWhen(string key, object? value, Func<object?, bool> predicate);

    /// <summary>Adds a typed value when <paramref name="predicate" /> returns true.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Placeholder name.</param>
    /// <param name="value">Value to bind.</param>
    /// <param name="predicate">When true for the value, it is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddWhen<T>(string key, T? value, Func<T?, bool> predicate);
}