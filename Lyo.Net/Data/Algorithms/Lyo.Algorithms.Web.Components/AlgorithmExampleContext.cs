using Lyo.Exceptions;

namespace Lyo.Algorithms.Web.Components;

/// <summary>Host sample rows for the context bag. The same keys show up in every algorithm editor on the host.</summary>
public sealed class AlgorithmExampleContext
{
    private readonly List<AlgorithmContextRow> _rows = [];

    /// <summary>Registered sample rows.</summary>
    public IReadOnlyList<AlgorithmContextRow> Rows => _rows;

    /// <summary>Adds or replaces a sample. <paramref name="alias" /> selects the value editor.</summary>
    public AlgorithmExampleContext Add(string name, string alias, string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(alias);
        _rows.RemoveAll(row => string.Equals(row.Name, name, StringComparison.OrdinalIgnoreCase));
        _rows.Add(new() { Name = name, Alias = alias, Text = text ?? string.Empty });
        return this;
    }
}
