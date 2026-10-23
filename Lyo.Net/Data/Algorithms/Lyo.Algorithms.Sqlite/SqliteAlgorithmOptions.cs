using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Sqlite;

namespace Lyo.Algorithms.Sqlite;

/// <summary>Settings for the SQLite algorithm catalog. Section <see cref="SectionName" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SqliteAlgorithmOptions : ISqliteMigrationConfig
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "SqliteAlgorithms";

    /// <summary>SQLite connection string, for example <c>Data Source=./algorithms.db</c>.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>When true, applies migrations when the host starts. Default false.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <summary>Throws when <see cref="ConnectionString" /> is missing.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(ConnectionString);

    /// <summary>Auto-migrate flag. The connection string is omitted.</summary>
    public override string ToString() => $"EnableAutoMigrations={EnableAutoMigrations}";
}