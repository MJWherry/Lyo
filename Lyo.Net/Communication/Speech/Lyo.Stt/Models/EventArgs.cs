using System.Diagnostics;

namespace Lyo.Stt.Models;

/// <summary>Raised as speech recognition is about to begin.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttRecognizingEventArgs(SttRequest SttRequest)
{
    public override string ToString() => SttRequest.ToString();
}

/// <summary>Raised as a bulk recognition is about to begin.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttBulkRecognizingEventArgs(IReadOnlyList<SttRequest> BulkSttRequests)
{
    public override string ToString() => "Bulk STT Requests Count: " + BulkSttRequests.Count;
}

/// <summary>Raised after speech recognition finishes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttRecognizedEventArgs(SttResult SttResult)
{
    public override string ToString() => SttResult.ToString();
}

/// <summary>Raised after a bulk recognition finishes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttBulkRecognizedEventArgs(IReadOnlyList<SttResult> BulkSttResults, TimeSpan ElapsedTime)
{
    public override string ToString() => $"Bulk STT Results Count: {BulkSttResults.Count}, Elapsed: {ElapsedTime:g}";
}