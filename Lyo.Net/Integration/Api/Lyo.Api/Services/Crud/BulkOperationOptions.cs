using System.Diagnostics;

namespace Lyo.Api.Services.Crud;

/// <summary>Singleton limits for bulk CRUD operations: max items per request, parallelism, and overall timeout.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BulkOperationOptions
{
    /// <summary>Max concurrent tasks when <see cref="UseParallelProcessing" /> is enabled.</summary>
    public int MaxDegreeOfParallelism { get; init; } = 10;

    /// <summary>When <c>true</c>, bulk handlers may process items in parallel (subject to <see cref="MaxDegreeOfParallelism" />).</summary>
    public bool UseParallelProcessing { get; init; } = true;

    /// <summary>Upper bound on time allowed for a single bulk operation.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Maximum number of items accepted in one bulk request body.</summary>
    public int MaxAmount { get; init; } = 2000;

    public override string ToString() => $"UseParallel={UseParallelProcessing} MaxParallel={MaxDegreeOfParallelism} MaxAmount={MaxAmount}";
}