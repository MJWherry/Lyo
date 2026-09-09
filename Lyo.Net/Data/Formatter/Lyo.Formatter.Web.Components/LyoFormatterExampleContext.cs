using Lyo.Exceptions;

namespace Lyo.Formatter.Web.Components;

/// <summary>
/// Host-registered sample data for formatter editors: keys a template can use that only the host knows about, such as objects a job worker binds with
/// <c>AddContext</c> at run time.
/// </summary>
/// <remarks>
/// Templates are written long before they run, and the keys they may use come from several places at once — the scheduler, the worker, and the host application.
/// An author cannot be expected to remember them, so the host declares its own contribution once at startup and every formatter editor picks it up. Views layer
/// their own live keys on top; see <see cref="Merge" />.
/// </remarks>
public sealed class LyoFormatterExampleContext
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registered keys. Values are sample objects, dictionaries, or anonymous objects; nested members show up as dotted paths in the editor.</summary>
    public IReadOnlyDictionary<string, object?> Values => _values;

    /// <summary>Adds or overwrites a root key. Fluent, so a host can chain several in one <c>AddLyoFormatterValueEditor</c> call.</summary>
    public LyoFormatterExampleContext Add(string key, object? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _values[key] = value;
        return this;
    }

    /// <summary>
    /// Merges the host default with a view's own context; the view's keys win on collision.
    /// </summary>
    /// <remarks>
    /// A dictionary can be merged key by key. Any other object cannot — its keys are properties, and grafting host keys onto it would need a wrapper that changes
    /// every path the user sees. A non-dictionary view context therefore replaces the host default outright.
    /// </remarks>
    /// <param name="host">Host default, typically resolved from DI. Null when the host registered nothing.</param>
    /// <param name="view">Context supplied by the view. Null when the view has no live keys of its own.</param>
    /// <returns>The context to format against, or null when neither side supplied anything.</returns>
    public static object? Merge(LyoFormatterExampleContext? host, object? view)
    {
        if (host is null || host._values.Count == 0)
            return view;

        if (view is null)
            return host._values;

        if (view is not IReadOnlyDictionary<string, object?> viewValues)
            return view;

        var merged = new Dictionary<string, object?>(host._values, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in viewValues)
            merged[key] = value;

        return merged;
    }
}
