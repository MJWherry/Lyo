namespace Lyo.Query.Models.Enums;

/// <summary>Aggregate applied to each group of a <c>QueryProject</c> request. <see cref="Count" /> is the row count. The others read <c>AggregateSelect.Field</c>.</summary>
public enum AggregateFunction
{
    /// <summary>SQL <c>COUNT(*)</c>. Null rows are included. <c>Field</c> is not used.</summary>
    Count,

    /// <summary>Sum of a numeric field. Nulls are skipped.</summary>
    Sum,

    /// <summary>Average of a numeric field. Nulls are skipped. Integral sources return <see cref="double" />; <see cref="decimal" /> stays <see cref="decimal" />.</summary>
    Average,

    /// <summary>Smallest value of a scalar field.</summary>
    Min,

    /// <summary>Largest value of a scalar field.</summary>
    Max,

    /// <summary>Count of distinct values of a scalar field. Nulls are skipped.</summary>
    CountDistinct
}