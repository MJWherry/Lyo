namespace Lyo.Reporting.Models.Rendering;

/// <summary>Optional callbacks around report generation. Consumers use hooks to persist or post-process staged output.</summary>
public sealed class ReportGenerationHooks
{
    public Func<ReportGenerateContext, CancellationToken, ValueTask>? BeforeGenerateAsync { get; init; }

    public Func<ReportGenerateContext, CancellationToken, ValueTask>? AfterRenderAsync { get; init; }

    public Func<ReportGenerateContext, CancellationToken, ValueTask>? AfterSaveAsync { get; init; }

    public Func<ReportGenerateFailureContext, CancellationToken, ValueTask>? OnFailureAsync { get; init; }

    /// <summary>
    /// Invoked before a generation row is removed (retention cleanup, generation delete, or definition delete) so hosts can delete the persisted output (e.g. FileStorage blob).
    /// Throwing skips deletion of that row.
    /// </summary>
    public Func<ReportCleanupContext, CancellationToken, ValueTask>? OnCleanupAsync { get; init; }
}

/// <summary>Context for <see cref="ReportGenerationHooks.OnCleanupAsync" />: a generation row about to be removed.</summary>
public sealed class ReportCleanupContext
{
    public required Guid GenerationId { get; init; }

    /// <summary>Persisted output id (consumer-defined, e.g. FileStorage). Null when the generation produced no saved output.</summary>
    public Guid? OutputFileId { get; init; }

    public string? PathPrefix { get; init; }

    public required IServiceProvider Services { get; init; }
}