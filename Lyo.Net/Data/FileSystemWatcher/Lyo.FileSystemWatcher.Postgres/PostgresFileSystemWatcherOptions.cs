using Lyo.Hashing;
using Lyo.Postgres;

namespace Lyo.FileSystemWatcher.Postgres;

/// <summary>Settings for the FileSystemWatcher PostgreSQL schema.</summary>
public sealed class PostgresFileSystemWatcherOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresFileSystemWatcher";

    /// <summary>PostgreSQL schema that owns watcher tables.</summary>
    public const string Schema = "filesystem_watcher";

    /// <summary>Digest used to hash snapshot JSON for watch dedupe. Default SHA-256.</summary>
    public ContentDigestAlgorithm ContentHashAlgorithm { get; set; } = ContentDigestAlgorithm.Sha256;

    /// <inheritdoc />
    protected override string SchemaName => Schema;

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        if (!Enum.IsDefined(ContentHashAlgorithm))
            throw new ArgumentException($"{nameof(ContentHashAlgorithm)} is not a known digest.", nameof(ContentHashAlgorithm));
    }
}
