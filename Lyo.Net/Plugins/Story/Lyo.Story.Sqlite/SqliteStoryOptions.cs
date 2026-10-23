using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Sqlite;

namespace Lyo.Story.Sqlite;

/// <summary>Settings for the local SQLite story database. Section <see cref="SectionName" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SqliteStoryOptions : ISqliteMigrationConfig
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "SqliteStory";

    /// <summary>SQLite connection string, for example <c>Data Source=./story.db</c>.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>When true, a generic host applies migrations on startup. Hosts without one call <c>MigrateSqliteStoryAsync</c>.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <summary>Throws when <see cref="ConnectionString" /> is missing.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(ConnectionString);

    /// <summary>Auto-migrate flag. The connection string is omitted.</summary>
    public override string ToString() => $"EnableAutoMigrations={EnableAutoMigrations}";
}