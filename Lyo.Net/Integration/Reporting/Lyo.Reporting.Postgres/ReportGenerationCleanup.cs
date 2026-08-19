using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Runs the host <see cref="ReportGenerationHooks.OnCleanupAsync" /> hook for generations whose rows are about to be removed outside retention (generation delete or definition
/// delete cascades). A hook failure propagates so the delete aborts rather than orphaning the stored output.
/// </summary>
public static class ReportGenerationCleanup
{
    public static async ValueTask InvokeCleanupHooksAsync(
        IEnumerable<ReportGeneration> generations,
        ReportGenerationHooks? hooks,
        IServiceProvider services,
        CancellationToken ct = default)
    {
        if (hooks?.OnCleanupAsync is null)
            return;

        foreach (var generation in generations.Where(g => g.OutputFileId is not null)) {
            await hooks.OnCleanupAsync(
                    new() {
                        GenerationId = generation.Id,
                        OutputFileId = generation.OutputFileId,
                        PathPrefix = generation.PathPrefix,
                        Services = services
                    }, ct)
                .ConfigureAwait(false);
        }
    }
}