using System.Diagnostics;
using Lyo.Common.Enums;

namespace Lyo.Query.Models.Common;

/// <summary>Specifies a sort key: dotted property path, direction, and optional ordering priority among multiple sorts.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SortBy
{
    /// <summary>Must match database entity property or decorate the response entity property with DatabaseNameAttribute</summary>
    public string PropertyName { get; set; }

    /// <summary>Ascending or descending order for <see cref="PropertyName" />.</summary>
    public SortDirection? Direction { get; set; }

    /// <summary>Optional. When omitted, sort order is determined by the list order in the request.</summary>
    public int? Priority { get; set; }

    /// <summary>Initializes a sort with an empty property name.</summary>
    public SortBy() => PropertyName = string.Empty;

    /// <summary>Initializes a sort for the given property, direction, and optional priority.</summary>
    public SortBy(string propertyName, SortDirection direction, int? priority = null)
    {
        PropertyName = propertyName;
        Direction = direction;
        Priority = priority;
    }

    public override string ToString() => $"{(Priority.HasValue ? Priority.Value.ToString() : "?")} - {PropertyName}, {Direction?.ToString()}";
}