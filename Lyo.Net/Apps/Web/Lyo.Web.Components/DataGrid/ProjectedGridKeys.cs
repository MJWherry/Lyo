namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Row-key selectors for projected grids. <c>LyoDataGridProjected</c> needs an <c>object[]</c> key per row so selection and expansion survive a refresh; projected rows are loose
/// dictionaries, so the key has to be read by field name.
/// </summary>
public static class ProjectedGridKeys
{
    /// <summary>Default id field name used in projections.</summary>
    public const string IdField = "Id";

    /// <summary>
    /// Key from an id field, normalized to <see cref="Guid" /> when it parses. Normalizing matters because the same row can arrive as a <see cref="Guid" /> from one projection and
    /// as a string from another; without it the grid treats them as distinct rows.
    /// </summary>
    /// <param name="item">Projected row.</param>
    /// <param name="field">Field holding the identifier.</param>
    public static object[] GuidId(object? item, string field = IdField)
    {
        var id = ProjectedValueHelper.GetValue(item, field);
        return ProjectedValueHelper.TryGetGuid(id, out var guid) ? [guid] : id != null ? [id] : [];
    }

    /// <summary>Key from an id field, left exactly as projected. Use for identifiers that are not Guid values.</summary>
    /// <param name="item">Projected row.</param>
    /// <param name="field">Field holding the identifier.</param>
    public static object[] RawId(object? item, string field = IdField)
    {
        var id = ProjectedValueHelper.GetValue(item, field);
        return id != null ? [id] : [];
    }

    /// <summary>Composite key for rows without a single identifier. Returns an empty key unless every field is present, since a partial key would collide across rows.</summary>
    /// <param name="item">Projected row.</param>
    /// <param name="fields">Fields that together identify the row.</param>
    public static object[] Composite(object? item, params string[] fields)
    {
        if (item == null || fields.Length == 0)
            return [];

        var key = new object[fields.Length];
        for (var i = 0; i < fields.Length; i++) {
            var value = ProjectedValueHelper.GetValue(item, fields[i]);
            if (value == null)
                return [];

            key[i] = value;
        }

        return key;
    }

    /// <summary>
    /// Stand-in projected rows from selected keys so bulk handlers can read an id field without depending on current-page row objects, which are replaced on every reload.
    /// </summary>
    /// <param name="keys">Selected keys from <c>LyoDataGridProjected.SelectedKeys</c>.</param>
    /// <param name="field">Field name to populate on each stand-in row. Defaults to <see cref="IdField" />.</param>
    public static IReadOnlyList<object?> RowsFromKeys(IReadOnlyList<object[]>? keys, string field = IdField)
    {
        if (keys is not { Count: > 0 })
            return [];

        var rows = new List<object?>();
        foreach (var key in keys) {
            if (key.Length == 0)
                continue;

            rows.Add(new Dictionary<string, object?> { [field] = key[0] });
        }

        return rows;
    }
}
