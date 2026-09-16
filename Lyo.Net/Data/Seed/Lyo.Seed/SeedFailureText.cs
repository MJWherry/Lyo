using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Seed;

/// <summary>Turns a save failure into the Postgres constraint, table, and entity types instead of EF's wrapper sentence.</summary>
public static class SeedFailureText
{
    /// <summary>
    /// Walks <paramref name="exception"/> and each inner exception. A <see cref="DbUpdateException"/> adds the entity type names being saved.
    /// A <see cref="PostgresException"/> adds SQL state, table, column, constraint, and detail.
    /// </summary>
    public static string Format(Exception exception)
    {
        ArgumentHelpers.ThrowIfNull(exception);
        var parts = new List<string>();
        for (var current = exception; current != null; current = current.InnerException)
            Add(parts, Describe(current));

        return string.Join(Environment.NewLine, parts);
    }

    private static string Describe(Exception exception) => exception switch {
        PostgresException postgres => DescribePostgres(postgres),
        DbUpdateException update => DescribeUpdate(update),
        _ => exception.Message
    };

    private static string DescribeUpdate(DbUpdateException update)
    {
        var names = update.Entries.Select(entry => entry.Entity.GetType().Name).Distinct().ToArray();
        return names.Length == 0 ? update.Message : $"{update.Message}{Environment.NewLine}Entities: {string.Join(", ", names)}";
    }

    private static string DescribePostgres(PostgresException postgres)
    {
        var parts = new List<string> { $"{postgres.SqlState}: {postgres.MessageText}" };
        if (!string.IsNullOrWhiteSpace(postgres.TableName))
            parts.Add($"table {postgres.TableName}");

        if (!string.IsNullOrWhiteSpace(postgres.ColumnName))
            parts.Add($"column {postgres.ColumnName}");

        if (!string.IsNullOrWhiteSpace(postgres.ConstraintName))
            parts.Add($"constraint {postgres.ConstraintName}");

        if (!string.IsNullOrWhiteSpace(postgres.Detail))
            parts.Add(postgres.Detail);

        return string.Join(", ", parts);
    }

    private static void Add(List<string> parts, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            parts.Add(text);
    }
}
