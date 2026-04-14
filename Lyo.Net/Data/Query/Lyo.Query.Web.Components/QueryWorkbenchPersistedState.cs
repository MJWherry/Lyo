using Lyo.Query.Models.Common.Request;

namespace Lyo.Query.Web.Components;

/// <summary>Server-persisted query workbench state (request + run targets). Response is never stored.</summary>
public sealed class QueryWorkbenchPersistedState
{
    /// <summary><c>/Query</c> body (full entities). Omitted in older saved state — derived from <see cref="QueryRequest"/> on load.</summary>
    public QueryReq? EntityQuery { get; set; }

    public ProjectionQueryReq QueryRequest { get; set; } = new() { Start = 0, Amount = 20 };

    public List<string> IncludeAll { get; set; } = [];

    public List<string> SelectAll { get; set; } = [];

    public List<string> KeysAll { get; set; } = [];

    public QueryWorkbenchRunConfiguration Run { get; set; } = new();
}
