using System.Diagnostics;

namespace Lyo.Tts.Models;

/// <summary>Event arguments for TTS synthesizing events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsSynthesizingEventArgs<TRequest>(TRequest TtsRequest)
    where TRequest : TtsRequest
{
    public override string ToString() => TtsRequest.ToString()!;
}

/// <summary>Event arguments for bulk TTS synthesizing events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsBulkSynthesizingEventArgs<TRequest>(IReadOnlyList<TRequest> BulkTtsRequests)
    where TRequest : TtsRequest
{
    public override string ToString() => "Bulk TTS Requests Count: " + BulkTtsRequests.Count;
}

/// <summary>Event arguments for TTS synthesized events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsSynthesizedEventArgs<TRequest>(TtsResult<TRequest> TtsResult)
    where TRequest : TtsRequest
{
    public override string ToString() => TtsResult.ToString();
}

/// <summary>Event arguments for bulk TTS synthesized events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsBulkSynthesizedEventArgs<TRequest>(IReadOnlyList<TtsResult<TRequest>> BulkTtsResults, TimeSpan ElapsedTime)
    where TRequest : TtsRequest
{
    public override string ToString() => $"Bulk TTS Results Count: {BulkTtsResults.Count}, Elapsed: {ElapsedTime:g}";
}