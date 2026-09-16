namespace Lyo.Query.Sql;

/// <summary>One positional parameter in a <see cref="SqlQuery" />. <see cref="Name" /> is <c>$1</c>, <c>$2</c>, and so on.</summary>
/// <param name="Name">Parameter name as it appears in <see cref="SqlQuery.Text" />.</param>
/// <param name="Value">Bound value. Not interpolated into <see cref="SqlQuery.Text" />.</param>
public sealed record SqlParameter(string Name, object? Value);

/// <summary>Parameterized Postgres text. Nothing has been executed.</summary>
/// <param name="Text">Command text with <c>$n</c> placeholders.</param>
/// <param name="Parameters">Values for those placeholders, in order.</param>
public sealed record SqlQuery(string Text, IReadOnlyList<SqlParameter> Parameters);
