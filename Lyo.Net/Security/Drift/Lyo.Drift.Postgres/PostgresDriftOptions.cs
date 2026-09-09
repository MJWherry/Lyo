using Lyo.Hashing;
using Lyo.Postgres;

namespace Lyo.Drift.Postgres;

/// <summary>Settings for the Drift PostgreSQL schema and ingest limits.</summary>
public sealed class PostgresDriftOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresDrift";

    /// <summary>PostgreSQL schema that owns drift tables.</summary>
    public const string Schema = "drift";

    /// <summary>Maximum JSON payload size accepted for a structure snapshot. Bodies over this size are rejected.</summary>
    public int MaxSnapshotJsonBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>Digest used to hash snapshot JSON for lineage dedupe. Default SHA-256. Changing this starts a new hash lineage.</summary>
    public ContentDigestAlgorithm ContentHashAlgorithm { get; set; } = ContentDigestAlgorithm.Sha256;

    /// <summary>
    /// When set, <see cref="DriftRetentionService" /> deletes snapshots and change events older than this, keeping the latest row per lineage.
    /// Null disables pruning.
    /// </summary>
    public TimeSpan? SnapshotRetention { get; set; }

    /// <inheritdoc />
    protected override string SchemaName => Schema;

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        if (MaxSnapshotJsonBytes <= 0)
            throw new ArgumentException($"{nameof(MaxSnapshotJsonBytes)} must be positive.", nameof(MaxSnapshotJsonBytes));
        if (!Enum.IsDefined(ContentHashAlgorithm))
            throw new ArgumentException($"{nameof(ContentHashAlgorithm)} is not a known digest.", nameof(ContentHashAlgorithm));
    }
}
