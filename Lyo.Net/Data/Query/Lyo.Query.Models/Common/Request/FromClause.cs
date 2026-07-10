using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Root or join source: mapped EF entity type + alias + optional nested filter scope.</summary>
[DebuggerDisplay("{Alias}:{EntityType}")]
public class FromClause
{
    /// <summary>SQL/range alias used in Select, Where, Sort, and Join ON paths (e.g. <c>o</c>, <c>p</c>).</summary>
    public string Alias { get; set; } = "";

    /// <summary>CLR entity type name as registered on the host DbContext (same as dynamic CRUD route segment, e.g. <c>OrderEntity</c>).</summary>
    public string EntityType { get; set; } = "";

    /// <summary>Optional Where/Keys applied to this source before join.</summary>
    public SourceQueryScope? Query { get; set; }
}
