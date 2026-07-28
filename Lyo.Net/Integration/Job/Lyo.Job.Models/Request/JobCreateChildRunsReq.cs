using System.Diagnostics;

namespace Lyo.Job.Models.Request;

/// <summary>Request body for fan-out batch child run creation under a parent run.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobCreateChildRunsReq
{
    /// <summary>Child run specifications. Each entry becomes a queued run linked to the parent.</summary>
    public List<JobChildRunSpec> Children { get; set; } = [];

    public override string ToString() => $"Children={Children.Count}";
}

/// <summary>Specification for a single child run within a batch.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobChildRunSpec
{
    /// <summary>Zero-based index within the batch.</summary>
    public int BatchIndex { get; set; }

    /// <summary>Optional parameter overrides for this child. When empty, parent parameters are copied.</summary>
    public List<JobRunParameterReq> Parameters { get; set; } = [];

    public override string ToString() => $"BatchIndex={BatchIndex} Parameters={Parameters.Count}";
}