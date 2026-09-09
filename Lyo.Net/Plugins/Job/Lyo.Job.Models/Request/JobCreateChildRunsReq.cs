using System.Diagnostics;

namespace Lyo.Job.Models.Request;

/// <summary>Request body for creating fan-out batch child runs under a parent run.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobCreateChildRunsReq
{
    /// <summary>Child run specifications. Each entry becomes a queued run tied to the parent.</summary>
    public List<JobChildRunSpec> Children { get; set; } = [];

    public override string ToString() => $"Children={Children.Count}";
}

/// <summary>Specification for one child run inside a batch.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobChildRunSpec
{
    /// <summary>Zero-based index inside the batch.</summary>
    public int BatchIndex { get; set; }

    /// <summary>Optional parameter overrides for this child. When empty, the parent parameters are copied.</summary>
    public List<JobRunParameterReq> Parameters { get; set; } = [];

    public override string ToString() => $"BatchIndex={BatchIndex} Parameters={Parameters.Count}";
}