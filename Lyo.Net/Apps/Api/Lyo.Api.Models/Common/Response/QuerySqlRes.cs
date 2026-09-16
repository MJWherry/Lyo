namespace Lyo.Api.Models.Common.Response;

/// <summary>SQL preview for a concrete, projection, or root query. The statement was not executed.</summary>
public sealed class QuerySqlRes
{
    /// <summary>Postgres command text with <c>$n</c> placeholders.</summary>
    public string Sql { get; set; } = "";

    /// <summary>Bound values for <see cref="Sql" />, in order.</summary>
    public List<QuerySqlParameter> Parameters { get; set; } = [];
}

/// <summary>One positional parameter from a <see cref="QuerySqlRes" />.</summary>
public sealed class QuerySqlParameter
{
    /// <summary>Placeholder name, <c>$1</c>, <c>$2</c>, and so on.</summary>
    public string Name { get; set; } = "";

    /// <summary>Bound value. Not interpolated into the command text.</summary>
    public object? Value { get; set; }
}
