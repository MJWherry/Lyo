using System.Diagnostics;
using System.Text.Json;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for tables.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TableBuilder
{
    private static readonly JsonSerializerOptions OptionsJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly Table _table = new();

    /// <summary>Sets the table's title.</summary>
    public TableBuilder SetTitle(string title)
    {
        _table.Title = title;
        return this;
    }

    /// <summary>Sets the table's caption.</summary>
    public TableBuilder SetCaption(string caption)
    {
        _table.Caption = caption;
        return this;
    }

    /// <summary>Sets whether column headers are shown.</summary>
    public TableBuilder SetShowHeaders(bool showHeaders = true)
    {
        _table.ShowHeaders = showHeaders;
        return this;
    }

    /// <summary>Sets whether row numbers are shown.</summary>
    public TableBuilder SetShowRowNumbers(bool showRowNumbers = true)
    {
        _table.ShowRowNumbers = showRowNumbers;
        return this;
    }

    /// <summary>Sets whether the table uses striped rows.</summary>
    public TableBuilder SetStriped(bool striped = true)
    {
        _table.Striped = striped;
        return this;
    }

    /// <summary>Sets whether the table draws borders.</summary>
    public TableBuilder SetBordered(bool bordered = true)
    {
        _table.Bordered = bordered;
        return this;
    }

    /// <summary>Rows come from a report parameter whose value is a JSON array of objects.</summary>
    public TableBuilder SetFromParameter(string parameterKey, Dictionary<string, string>? fieldMap = null)
    {
        _table.DataSourceKind = DataSourceKind.FromParameter;
        _table.DataParameterKey = parameterKey;
        _table.FieldMap = fieldMap ?? [];
        return this;
    }

    /// <summary>Rows come from a stored procedure. Writes ParameterOptions JSON onto <see cref="Table.Options" />.</summary>
    public TableBuilder SetFromSproc(string storedProcName, IReadOnlyDictionary<string, string>? parameters = null)
    {
        _table.DataSourceKind = DataSourceKind.Sproc;
        var payload = new Dictionary<string, object?> { ["kind"] = "Sproc", ["storedProcName"] = storedProcName };
        if (parameters is { Count: > 0 })
            payload["sprocParameters"] = parameters;

        _table.Options = JsonSerializer.Serialize(payload, OptionsJson);
        return this;
    }

    /// <summary>Rows come from a root query. <paramref name="optionsJson" /> is <c>ParameterOptions</c> JSON.</summary>
    public TableBuilder SetFromQuery(string optionsJson)
    {
        _table.DataSourceKind = DataSourceKind.Query;
        _table.Options = optionsJson;
        return this;
    }

    /// <summary>Adds a CSS style on the table.</summary>
    public TableBuilder AddStyle(string property, string value)
    {
        _table.Styles[property] = value;
        return this;
    }

    /// <summary>When true, print/PDF keeps the table on one page. Default (unset) allows the table to split.</summary>
    public TableBuilder SetKeepTogether(bool keepTogether = true)
    {
        _table.KeepTogether = keepTogether;
        return this;
    }

    /// <summary>Appends a column definition to the table.</summary>
    public TableBuilder AddColumn(string header, string? field = null, string? width = null, string? alignment = null)
    {
        _table.Columns.Add(
            new() {
                Header = header,
                Field = field,
                Width = width,
                Alignment = alignment
            });

        return this;
    }

    /// <summary>Appends a column definition via a column builder.</summary>
    public TableBuilder AddColumn(Action<TableColumnBuilder> configure)
    {
        var builder = new TableColumnBuilder();
        configure(builder);
        _table.Columns.Add(builder.Build());
        return this;
    }

    /// <summary>Appends a column definition from a column builder instance.</summary>
    public TableBuilder AddColumn(TableColumnBuilder columnBuilder)
    {
        _table.Columns.Add(columnBuilder.Build());
        return this;
    }

    /// <summary>Appends several column definitions from headers.</summary>
    public TableBuilder AddColumns(params string[] headers)
    {
        foreach (var header in headers)
            AddColumn(header, header);

        return this;
    }

    /// <summary>Appends a row to the table.</summary>
    public TableBuilder AddRow(params object?[] cells)
    {
        _table.Rows.Add(new() { Cells = cells.ToList() });
        return this;
    }

    /// <summary>Appends a row to the table.</summary>
    public TableBuilder AddRow(IEnumerable<object?> cells)
    {
        _table.Rows.Add(new() { Cells = cells.ToList() });
        return this;
    }

    /// <summary>Appends a row via a row builder.</summary>
    public TableBuilder AddRow(Action<TableRowBuilder> configure)
    {
        var builder = new TableRowBuilder();
        configure(builder);
        _table.Rows.Add(builder.Build());
        return this;
    }

    /// <summary>Appends a row from a row builder instance.</summary>
    public TableBuilder AddRow(TableRowBuilder rowBuilder)
    {
        _table.Rows.Add(rowBuilder.Build());
        return this;
    }

    /// <summary>Appends several rows from a collection of objects, mapping properties onto columns.</summary>
    public TableBuilder AddRowsFromObjects<TItem>(IEnumerable<TItem> items, Func<TItem, object?[]>? mapper = null)
    {
        foreach (var item in items) {
            if (mapper != null)
                AddRow(mapper(item));
            else {
                var cells = new List<object?>();
                foreach (var col in _table.Columns) {
                    if (!string.IsNullOrEmpty(col.Field)) {
                        var prop = typeof(TItem).GetProperty(col.Field);
                        cells.Add(prop?.GetValue(item));
                    }
                    else
                        cells.Add(null);
                }

                AddRow(cells);
            }
        }

        return this;
    }

    /// <summary>Builds and returns the finished table.</summary>
    public Table Build() => _table;

    public override string ToString() => $"TableBuilder: {_table.Title ?? "(Untitled)"} ({_table.Columns.Count} columns, {_table.Rows.Count} rows)";
}
