namespace Lyo.DataTable.Models;

/// <summary>Fluent builder for column definitions with conditional formatting.</summary>
public sealed class DataTableColumnBuilder
{
    internal List<(Func<object?, bool> Predicate, Action<DataTableCellBuilder> Apply)> Rules { get; } = [];

    internal bool SumFooter { get; set; }

    /// <summary>Enables a sum footer for this column. Sum is computed at Build() time.</summary>
    public DataTableColumnBuilder WithSumFooter()
    {
        SumFooter = true;
        return this;
    }

    /// <summary>Applies formatting when the predicate returns true. Value is converted to T when evaluating (e.g. numbers, DateTime).</summary>
    public DataTableColumnBuilder FormatWhen<T>(Func<T, bool> when, Action<DataTableCellBuilder> apply)
    {
        Rules.Add((v => v is T t && when(t), apply));
        return this;
    }
}