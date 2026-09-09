using Lyo.Postgres;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>Options for the PostgreSQL file metadata store.</summary>
public sealed class PostgresFileMetadataStoreOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresFileMetadataStore";
    public const string Schema = "filestore";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}