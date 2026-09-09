using Lyo.Postgres;

namespace Lyo.Reporting.Postgres;

/// <summary>Settings for PostgreSQL reporting.</summary>
public sealed class PostgresReportingOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresReporting";
    public const string Schema = "reporting";

    /// <summary>Largest composition JSON accepted on generate (definition seed, override, or ad-hoc), in bytes.</summary>
    public int MaxReportDataJsonBytes { get; set; } = 5_000_000;

    /// <summary>Largest staged output file after render or pre-render, in bytes.</summary>
    public long MaxOutputFileBytes { get; set; } = 50_000_000;

    /// <summary>
    /// When false, generate requires a saved <c>ReportDefinitionId</c> and rejects ad-hoc <c>ReportDataJson</c> / <c>OverrideReportDataJson</c> payloads. Defaults to false.
    /// Composition JSON drives rendering, so accepting it from a request means anyone authorized to generate can author report markup. Hosts that intentionally offer ad-hoc
    /// generation must opt in.
    /// </summary>
    public bool AllowAdHocGeneration { get; set; }

    /// <summary>
    /// Filesystem roots that a data provider's <c>PreRenderedFilePath</c> may live under. Generation copies that path into its staging session, so without confinement a
    /// compromised or careless provider could stage any file the host can read. Empty falls back to the OS temp directory, where providers normally stage output. Symlinks are
    /// resolved before the check, and traversal outside a root is rejected.
    /// </summary>
    public IList<string> PreRenderedFileRoots { get; set; } = [];

    /// <summary>
    /// Most report generations running concurrently per host. 0 (default) means unlimited. When the limit is reached, generate waits briefly and then fails with a "busy" error.
    /// </summary>
    public int MaxConcurrentGenerations { get; set; }

    /// <summary>
    /// Age after which terminal (Succeeded/Failed) generations become eligible for <see cref="ReportRetentionService" /> cleanup. Null (default) disables retention cleanup.
    /// </summary>
    public TimeSpan? GenerationRetention { get; set; }

    /// <summary>
    /// Wall-clock limit for one generation (data provider, render, and hooks). When exceeded, the run is cancelled and persisted as Failed. Null disables the limit. Default
    /// 10 minutes.
    /// </summary>
    public TimeSpan? GenerationTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Age after which non-terminal (Pending/Running) generations are treated as stuck (for example the host crashed mid-generation) and are marked Failed by
    /// <see cref="ReportRetentionService" />. Null disables recovery. Default 1 hour. Should exceed <see cref="GenerationTimeout" />.
    /// </summary>
    public TimeSpan? StuckGenerationTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How often the opt-in maintenance worker (<c>AddReportingMaintenanceWorker</c>) runs retention cleanup and stuck-generation recovery. Defaults to 15 minutes.
    /// </summary>
    public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <inheritdoc />
    protected override string SchemaName => Schema;

    public override void Validate()
    {
        base.Validate();

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

        foreach (var root in PreRenderedFileRoots) {
            if (string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root))
                throw new ArgumentException($"{nameof(PreRenderedFileRoots)} entries must be absolute paths; got '{root}'.", nameof(PreRenderedFileRoots));
        }
    }
}