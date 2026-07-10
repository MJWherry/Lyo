namespace Lyo.Query.Web.Components;

public enum QueryWorkbenchRunMode
{
    /// <summary>Typed/dynamic entity graph: <c>POST …/QueryConcrete</c>.</summary>
    Query = 0,

    /// <summary>Typed/dynamic nav projection: <c>POST …/QueryProject</c>.</summary>
    QueryProject = 1,

    /// <summary>Root From/Joins query: <c>POST {dynamicBase}/Query</c>.</summary>
    RootQuery = 2
}
