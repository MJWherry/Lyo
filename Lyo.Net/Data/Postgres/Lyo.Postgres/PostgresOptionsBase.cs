using Lyo.Exceptions;

namespace Lyo.Postgres;

/// <summary>Shared options base for a Lyo package that stores data in PostgreSQL.</summary>
/// <remarks>
/// Holds the three fields every <see cref="IPostgresMigrationConfig" /> needs. A derived class then adds only what belongs to that package: <c>SectionName</c> and
/// <c>Schema</c> constants, the <see cref="SchemaName" /> override, plus any extra knobs.
/// <para>
/// The schema is exposed through <see cref="SchemaName" /> instead of a <c>Schema</c> property so derived types can still declare <c>public const string Schema</c>.
/// Callers use that constant without building options (migration factories, test fixtures, <c>MigrationsHistoryTable</c> calls).
/// </para>
/// </remarks>
public abstract class PostgresOptionsBase : IPostgresMigrationConfig
{
    /// <summary>PostgreSQL connection string. Required. <see cref="Validate" /> rejects a missing value.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>If true, pending migrations run when the host starts. Defaults to <c>false</c>; the host must opt in.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <summary>PostgreSQL schema that owns this package's tables and its migration-history table.</summary>
    protected abstract string SchemaName { get; }

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => SchemaName;

    /// <summary>Throws if these options cannot open a connection.</summary>
    /// <remarks>
    /// <c>AddPostgresDbContextFactory</c> calls this before registration so a bad host config fails at startup rather than on the first query. Override to add
    /// package checks, and call <c>base.Validate()</c> first so the connection-string check still runs.
    /// </remarks>
    /// <exception cref="ArgumentException">Raised when <see cref="ConnectionString" /> is null, empty, or whitespace.</exception>
    public virtual void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(ConnectionString, nameof(ConnectionString));
}
