namespace Lyo.Parameters;

/// <summary>
/// Named string-encoded value. The minimum a collection must expose for the typed accessors in <see cref="LyoKeyedValueExtensions" /> to work over it, and is
/// implemented by job/report parameters, run results, and anything else that stores values as JSON or plain text keyed by name.
/// </summary>
public interface ILyoKeyedValue
{
    /// <summary>Name used to look the value up. Lookups are case-insensitive.</summary>
    string Key { get; }

    /// <summary>Serialized value, or null when unset. Scalars are stored as plain text; complex types as JSON.</summary>
    string? Value { get; }
}
