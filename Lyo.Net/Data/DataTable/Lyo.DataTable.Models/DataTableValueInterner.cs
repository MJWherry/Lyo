namespace Lyo.DataTable.Models;

/// <summary>
/// Parse-scoped interner for cell values and formats.
/// Create one instance per parse (single-threaded); do not use <see cref="string.Intern" />.
/// </summary>
public sealed class DataTableValueInterner
{
    private readonly Dictionary<DataTableCellFormat, DataTableCellFormat>? _formats;
    private readonly Dictionary<string, string>? _values;

    /// <summary>Creates an interner according to <paramref name="options" /> and estimated cell count.</summary>
    /// <param name="options">Pooling options. When null, defaults are used.</param>
    /// <param name="estimatedCellCount">Estimated cells for the threshold gate (used-range / cols×rows upper bound is fine).</param>
    public DataTableValueInterner(DataTablePoolingOptions? options, int estimatedCellCount)
    {
        options ??= new();
        options.Validate();
        var aboveThreshold = options.PoolingCellThreshold == 0 || estimatedCellCount >= options.PoolingCellThreshold;
        if (options.PoolValues && aboveThreshold)
            _values = new(StringComparer.Ordinal);

        if (options.PoolFormats && aboveThreshold)
            _formats = new();
    }

    /// <summary>Whether value pooling is active for this parse.</summary>
    public bool PoolsValues => _values != null;

    /// <summary>Whether format pooling is active for this parse.</summary>
    public bool PoolsFormats => _formats != null;

    /// <summary>Returns a shared instance for equal strings when value pooling is on; otherwise returns <paramref name="value" />.</summary>
    public string Intern(string? value)
    {
        if (value == null)
            return "";

        if (value.Length == 0)
            return "";

        if (_values == null)
            return value;

        if (_values.TryGetValue(value, out var existing))
            return existing;

        _values[value] = value;
        return value;
    }

    /// <summary>Returns a shared instance for equal formats when format pooling is on; otherwise returns <paramref name="format" />.</summary>
    public DataTableCellFormat? Intern(DataTableCellFormat? format)
    {
        if (format == null)
            return null;

        if (_formats == null)
            return format;

        if (_formats.TryGetValue(format, out var existing))
            return existing;

        _formats[format] = format;
        return format;
    }
}
