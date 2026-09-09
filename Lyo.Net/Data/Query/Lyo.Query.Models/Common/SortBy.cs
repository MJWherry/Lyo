using System.Diagnostics;
using Lyo.Common.Core.Enums;

namespace Lyo.Query.Models.Common;

/// <summary>Sort key: dotted property path, direction, and optional priority among multiple sorts.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SortBy
{
    /// <summary>Must match a database entity property, or decorate the response entity property with DatabaseNameAttribute.</summary>
    public string PropertyName { get; set; }

    /// <summary>Ascending or descending for <see cref="PropertyName" />.</summary>
    public SortDirection? Direction { get; set; }

    /// <summary>Optional. When omitted, list order in the request sets sort order.</summary>
    public int? Priority { get; set; }

    /// <summary>Builds a sort with an empty property name.</summary>
    public SortBy() => PropertyName = string.Empty;

    /// <summary>Builds a sort for the given property, direction, and optional priority.</summary>
    public SortBy(string propertyName, SortDirection direction, int? priority = null)
    {
        PropertyName = propertyName;
        Direction = direction;
        Priority = priority;
    }

    public override string ToString() => $"{(Priority.HasValue ? Priority.Value.ToString() : "?")} - {PropertyName}, {Direction?.ToString()}";
}