namespace Lyo.Formatter;

/// <summary>Fluent builder for adding context values to a template, with support for custom formatting.</summary>
public interface IContextBuilder
{
    /// <summary>Adds a raw value. Use for strings, numbers, or objects that SmartFormat can format from the template.</summary>
    /// <param name="key">The placeholder name (e.g. "UserName" for "{UserName}").</param>
    /// <param name="value">The value.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add(string key, object? value);

    /// <summary>Adds a value with a format string. Uses IFormattable for DateTime, numeric types, etc. (e.g. "yyyy-MM-dd", "N2", "C").</summary>
    /// <param name="key">The placeholder name.</param>
    /// <param name="value">The value to format.</param>
    /// <param name="format">Format string (e.g. "yyyy-MM-dd" for DateTime, "N2" for decimals, "C" for currency).</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add(string key, object? value, string format);

    /// <summary>Adds a value with a custom formatter function for full control over the output string.</summary>
    /// <param name="key">The placeholder name.</param>
    /// <param name="value">The value.</param>
    /// <param name="formatter">Function that converts the value to a string. Receives the value; return null for empty.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add(string key, object? value, Func<object?, string?> formatter);

    /// <summary>Adds a value with a typed custom formatter for cleaner syntax with known types.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The placeholder name.</param>
    /// <param name="value">The value.</param>
    /// <param name="formatter">Function that converts the value to a string.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder Add<T>(string key, T? value, Func<T?, string?> formatter);

    /// <summary>Adds a value only when the condition is true.</summary>
    /// <param name="key">The placeholder name.</param>
    /// <param name="value">The value.</param>
    /// <param name="condition">When true, the value is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddIf(string key, object? value, bool condition);

    /// <summary>Adds a value only when the condition is true. Supports format string for IFormattable types.</summary>
    /// <param name="key">The placeholder name.</param>
    /// <param name="value">The value.</param>
    /// <param name="format">Format string (e.g. "yyyy-MM-dd", "N2").</param>
    /// <param name="condition">When true, the value is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddIf(string key, object? value, string format, bool condition);

    /// <summary>Adds a value when the predicate returns true for the value.</summary>
    /// <param name="key">The placeholder name.</param>
    /// <param name="value">The value.</param>
    /// <param name="predicate">When true for the value, it is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddWhen(string key, object? value, Func<object?, bool> predicate);

    /// <summary>Adds a typed value when the predicate returns true.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The placeholder name.</param>
    /// <param name="value">The value.</param>
    /// <param name="predicate">When true for the value, it is added; otherwise skipped.</param>
    /// <returns>This builder for chaining.</returns>
    IContextBuilder AddWhen<T>(string key, T? value, Func<T?, bool> predicate);
}