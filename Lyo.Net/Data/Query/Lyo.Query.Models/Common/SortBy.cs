using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Enums;

namespace Lyo.Query.Models.Common;

[DebuggerDisplay("{ToString(),nq}")]
public class SortBy
{
    /// <summary>Must match database entity property or decorate the response entity property with DatabaseNameAttribute</summary>
    public string PropertyName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortDirection? Direction { get; set; }

    /// <summary>Optional. When omitted, sort order is determined by the list order in the request.</summary>
    public int? Priority { get; set; }

    public SortBy() { }

    public SortBy(string propertyName, SortDirection direction, int? priority = null)
    {
        PropertyName = propertyName;
        Direction = direction;
        Priority = priority;
    }

    public override string ToString() => $"{(Priority.HasValue ? Priority.Value.ToString() : "?")} - {PropertyName}, {Direction?.ToString()}";
}