using System.Diagnostics;

namespace Lyo.Stt.Models;

/// <summary>Event arguments for STT recognizing events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttRecognizingEventArgs(SttRequest SttRequest)
{
    public override string ToString() => SttRequest.ToString();
}

/// <summary>Event arguments for bulk STT recognizing events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttBulkRecognizingEventArgs(IReadOnlyList<SttRequest> BulkSttRequests)
{
    public override string ToString() => "Bulk STT Requests Count: " + BulkSttRequests.Count;
}

/// <summary>Event arguments for STT recognized events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttRecognizedEventArgs(SttResult SttResult)
{
    public override string ToString() => SttResult.ToString();
}

/// <summary>Event arguments for bulk STT recognized events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SttBulkRecognizedEventArgs(IReadOnlyList<SttResult> BulkSttResults, TimeSpan ElapsedTime)
{
    public override string ToString() => $"Bulk STT Results Count: {BulkSttResults.Count}, Elapsed: {ElapsedTime:g}";
}