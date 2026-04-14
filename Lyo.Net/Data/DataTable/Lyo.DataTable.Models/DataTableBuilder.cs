using System.Globalization;

namespace Lyo.DataTable.Models;

/// <summary>Fluent builder for DataTable.</summary>
public sealed class DataTableBuilder
{
    private readonly Dictionary<int, DataTableColumnBuilder> _columnDefs = new();
    private readonly DataTable _table = new();

    /// <summary>Defines a column with optional conditional formatting (FormatWhen).</summary>
    public DataTableBuilder AddColumn(int col, Action<DataTableColumnBuilder> configure)
    {
        var builder = new DataTableColumnBuilder();
        configure(builder);
        _columnDefs[col] = builder;
        return this;
    }

    /// <summary>Adds a header at the given column.</summary>
    public DataTableBuilder AddHeader(int col, string value)
    {
        _table.SetHeader(col, DataTableCell.FromValue(value ?? ""));
        return this;
    }

    /// <summary>Adds a header at the given column.</summary>
    public DataTableBuilder AddHeader(int col, IDataTableCell cell)
    {
        _table.SetHeader(col, cell);
        return this;
    }

    /// <summary>Adds a header at the given column using a builder.</summary>
    public DataTableBuilder AddHeader(int col, DataTableCellBuilder builder)
    {
        _table.SetHeader(col, builder.Build());
        return this;
    }

    /// <summary>Adds headers for columns 0, 1, 2, ... from the given values.</summary>
    public DataTableBuilder AddHeaders(params string[] values)
    {
        for (var i = 0; i < values.Length; i++)
            _table.SetHeader(i, DataTableCell.FromValue(values[i] ?? ""));

        return this;
    }

    /// <summary>Adds a row.</summary>
    public DataTableBuilder AddRow(DataTableRow row)
    {
        var newRow = _table.AddRow();
        foreach (var kv in row.Cells)
            newRow.SetCell(kv.Key, kv.Value);

        return this;
    }

    /// <summary>Adds a row from a builder.</summary>
    public DataTableBuilder AddRow(DataTableRowBuilder builder)
    {
        AddRow(builder.Build());
        return this;
    }

    /// <summary>Adds a row configured by the given action.</summary>
    public DataTableBuilder AddRow(Action<DataTableRowBuilder> configure)
    {
        var rowBuilder = new DataTableRowBuilder(_columnDefs);
        configure(rowBuilder);
        AddRow(rowBuilder.Build());
        return this;
    }

    /// <summary>Returns a row builder for chaining cells. Call BuildAndAdd() when done to add the row and continue table building. FormatWhen rules apply when using SetCell&lt;T&gt;.</summary>
    public DataTableRowBuilder AddRow() => new(_columnDefs, this);

    /// <summary>Adds a footer at the given column.</summary>
    public DataTableBuilder AddFooter(int col, string value)
    {
        _table.SetFooter(col, DataTableCell.FromValue(value ?? ""));
        return this;
    }

    /// <summary>Adds a footer at the given column.</summary>
    public DataTableBuilder AddFooter(int col, IDataTableCell cell)
    {
        _table.SetFooter(col, cell);
        return this;
    }

    /// <summary>Adds a footer at the given column using a builder.</summary>
    public DataTableBuilder AddFooter(int col, DataTableCellBuilder builder)
    {
        _table.SetFooter(col, builder.Build());
        return this;
    }

    /// <summary>Adds footers for columns 0, 1, 2, ... from the given values.</summary>
    public DataTableBuilder AddFooters(params string[] values)
    {
        for (var i = 0; i < values.Length; i++)
            _table.SetFooter(i, DataTableCell.FromValue(values[i] ?? ""));

        return this;
    }

    /// <summary>Adds footer sums for the specified columns. Column indices are converted via IConvertible. Sums are computed at Build() time.</summary>
    public DataTableBuilder AddSumFooter(params IConvertible[] columns)
    {
        foreach (var col in columns) {
            var colIndex = col.ToInt32(CultureInfo.InvariantCulture);
            if (!_columnDefs.TryGetValue(colIndex, out var def)) {
                def = new();
                _columnDefs[colIndex] = def;
            }

            def.SumFooter = true;
        }

        return this;
    }

    private static decimal ToDecimal(IDataTableCell cell)
    {
        var value = cell.DisplayValue;
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        var s = value.Trim().TrimStart('$', '£', '€', '¥', ' ').Replace(",", "").Trim();
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            return d;

        try {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch {
            return 0m;
        }
    }

    /// <summary>Builds the DataTable. Applies sum footers for columns with WithSumFooter or AddSumFooter.</summary>
    public DataTable Build()
    {
        foreach (var kv in _columnDefs.Where(kv => kv.Value.SumFooter)) {
            var col = kv.Key;
            var sum = _table.Rows.Select(r => r.Cells.TryGetValue(col, out var c) ? c : DataTableCell.Empty).Sum(ToDecimal);
            _table.SetFooter(col, DataTableCell.FromValue(sum.ToString(CultureInfo.InvariantCulture)));
        }

        return _table;
    }
}