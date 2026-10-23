namespace Lyo.Query.Sql;

/// <summary>Raised when a query request cannot be printed as Postgres SQL. The request was not executed.</summary>
public sealed class SqlTranslationException(string message)
    : Exception(message);