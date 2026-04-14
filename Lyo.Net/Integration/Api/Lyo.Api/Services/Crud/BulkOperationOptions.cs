using System.Diagnostics;

namespace Lyo.Api.Services.Crud;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class BulkOperationOptions
{
    public int MaxDegreeOfParallelism { get; init; } = 10;

    public bool UseParallelProcessing { get; init; } = true;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    public int MaxAmount { get; init; } = 2000;

    public override string ToString() => $"UseParallel={UseParallelProcessing} MaxParallel={MaxDegreeOfParallelism} MaxAmount={MaxAmount}";
}