using Lyo.Exceptions;

namespace Lyo.Query.Sql;

/// <summary>Scalar table mapping the translator uses instead of an EF model. Property names are CLR names; column names are store names.</summary>
public sealed class SqlColumnMap
{
    /// <summary>Creates a map. <paramref name="schema" /> may be empty when the statement should not qualify the table.</summary>
    /// <param name="schema">Schema name, or empty for an unqualified table.</param>
    /// <param name="table">Table name.</param>
    /// <param name="columns">CLR property name to store column name. Enumeration order is the concrete <c>SELECT</c> order.</param>
    public SqlColumnMap(string schema, string table, IReadOnlyDictionary<string, string> columns)
    {
        ArgumentHelpers.ThrowIfNull(schema);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(table);
        ArgumentHelpers.ThrowIfNull(columns);
        Schema = schema;
        Table = table;
        var copy = new Dictionary<string, string>(columns.Count, StringComparer.Ordinal);
        foreach (var pair in columns)
            copy[pair.Key] = pair.Value;

        Columns = copy;
    }

    /// <summary>Schema, or empty when the table is unqualified.</summary>
    public string Schema { get; }

    /// <summary>Table name.</summary>
    public string Table { get; }

    /// <summary>CLR property name to store column name, in <c>SELECT</c> order for a concrete query.</summary>
    public IReadOnlyDictionary<string, string> Columns { get; }
}
