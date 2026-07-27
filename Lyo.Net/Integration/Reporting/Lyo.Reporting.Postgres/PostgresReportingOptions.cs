using Lyo.Postgres;

namespace Lyo.Reporting.Postgres;

/// <summary>Configuration options for PostgreSQL reporting.</summary>
public sealed class PostgresReportingOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresReporting";
    public const string Schema = "reporting";

    public string ConnectionString { get; set; } = string.Empty;

    public bool EnableAutoMigrations { get; set; }

    /// <summary>Max bytes for composition JSON accepted on generate (definition seed, override, or ad-hoc).</summary>
    public int MaxReportDataJsonBytes { get; set; } = 5_000_000;

    /// <summary>Max bytes for staged output file after render / pre-render.</summary>
    public long MaxOutputFileBytes { get; set; } = 50_000_000;

    /// <summary>
    /// When false, generate requires a saved <c>ReportDefinitionId</c> and rejects ad-hoc
    /// <c>ReportDataJson</c> / <c>OverrideReportDataJson</c> payloads. Default true (preserves prior behavior).
    /// </summary>
    public bool AllowAdHocGeneration { get; set; } = true;

    /// <summary>
    /// Max report generations running concurrently per host. 0 (default) means unlimited.
    /// When the limit is reached, generate waits briefly and then fails with a "busy" error.
    /// </summary>
    public int MaxConcurrentGenerations { get; set; }

    /// <summary>
    /// Age after which terminal (Succeeded/Failed) generations are eligible for <see cref="ReportRetentionService"/> cleanup.
    /// Null (default) disables retention cleanup.
    /// </summary>
    public TimeSpan? GenerationRetention { get; set; }

    /// <summary>
    /// Wall-clock limit for a single generation (data provider, render, and hooks). When exceeded, the run is
    /// cancelled and persisted as Failed. Null disables the limit. Default 10 minutes.
    /// </summary>
    public TimeSpan? GenerationTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Age after which non-terminal (Pending/Running) generations are considered stuck (e.g. the host crashed
    /// mid-generation) and are marked Failed by <see cref="ReportRetentionService"/>. Null disables recovery.
    /// Default 1 hour; should exceed <see cref="GenerationTimeout"/>.
    /// </summary>
    public TimeSpan? StuckGenerationTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How often the opt-in maintenance worker (<c>AddReportingMaintenanceWorker</c>) runs retention cleanup and
    /// stuck-generation recovery. Default 15 minutes.
    /// </summary>
    public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromMinutes(15);

    string IPostgresMigrationConfig.Schema => Schema;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new ArgumentException($"{nameof(PostgresReportingOptions)}.{nameof(ConnectionString)} is required.", nameof(ConnectionString));
        if (MaxReportDataJsonBytes <= 0)
            throw new ArgumentException($"{nameof(MaxReportDataJsonBytes)} must be positive.", nameof(MaxReportDataJsonBytes));
        if (MaxOutputFileBytes <= 0)
            throw new ArgumentException($"{nameof(MaxOutputFileBytes)} must be positive.", nameof(MaxOutputFileBytes));
        if (MaxConcurrentGenerations < 0)
            throw new ArgumentException($"{nameof(MaxConcurrentGenerations)} must be zero (unlimited) or positive.", nameof(MaxConcurrentGenerations));
        if (GenerationRetention is { } retention && retention <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(GenerationRetention)} must be positive when set.", nameof(GenerationRetention));
        if (GenerationTimeout is { } generationTimeout && generationTimeout <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(GenerationTimeout)} must be positive when set.", nameof(GenerationTimeout));
        if (StuckGenerationTimeout is { } stuckTimeout && stuckTimeout <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(StuckGenerationTimeout)} must be positive when set.", nameof(StuckGenerationTimeout));
        if (MaintenanceInterval <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(MaintenanceInterval)} must be positive.", nameof(MaintenanceInterval));
    }
}
