using Lyo.Api.Client;
using Lyo.Exceptions;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Collects <c>LyoIdColumn</c> descriptors during grid render so the parent load can batch related QueryProject calls.
/// Cleared and rebuilt each render, same as <see cref="ProjectedColumnRegistry" />.
/// </summary>
public sealed class RelatedColumnRegistry
{
    private readonly List<RelatedColumnDescriptor> _columns = [];

    /// <summary>Removes every registered related column. Call at the start of a grid render before child columns re-register.</summary>
    public void Clear() => _columns.Clear();

    /// <summary>Adds a related-column descriptor. <paramref name="select" /> should already include <c>Id</c>.</summary>
    public void Register(string field, string route, Type resType, IReadOnlyList<string> select, IApiClient? apiClient = null, bool hidden = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(field);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(route);
        ArgumentHelpers.ThrowIfNull(resType);
        ArgumentHelpers.ThrowIfNull(select);
        _columns.Add(new() {
            Field = field.Trim(),
            Route = route.Trim().TrimEnd('/'),
            ResType = resType,
            Select = select,
            ApiClient = apiClient,
            Hidden = hidden
        });
    }

    /// <summary>Updates hidden state after the user toggles a column so the next load can skip its related fetch.</summary>
    public void SetHidden(string field, bool hidden)
    {
        if (string.IsNullOrWhiteSpace(field))
            return;

        var name = field.Trim();
        foreach (var column in _columns) {
            if (column.Field.Equals(name, StringComparison.OrdinalIgnoreCase))
                column.Hidden = hidden;
        }
    }

    /// <summary>Registered columns that are currently visible (included in related fetches).</summary>
    public IReadOnlyList<RelatedColumnDescriptor> GetVisible() => _columns.Where(c => !c.Hidden).ToList();

    /// <summary>Every registered related column, including hidden ones.</summary>
    public IReadOnlyList<RelatedColumnDescriptor> GetAll() => _columns;
}
