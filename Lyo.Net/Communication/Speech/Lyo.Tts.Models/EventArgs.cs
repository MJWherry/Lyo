using System.Diagnostics;

namespace Lyo.Tts.Models;

/// <summary>Args for Synthesizing on <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="TtsRequest">Request that is about to run.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsSynthesizingEventArgs<TRequest>(TRequest TtsRequest)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => TtsRequest.ToString()!;
}

/// <summary>Args for BulkSynthesizing on <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="BulkTtsRequests">Items in this bulk run.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsBulkSynthesizingEventArgs<TRequest>(IReadOnlyList<TRequest> BulkTtsRequests)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => "Bulk TTS Requests Count: " + BulkTtsRequests.Count;
}

/// <summary>Args for Synthesized on <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="TtsResult">Outcome of one synthesis.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsSynthesizedEventArgs<TRequest>(TtsResult<TRequest> TtsResult)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => TtsResult.ToString();
}

/// <summary>Args for BulkSynthesized on <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="BulkTtsResults">Per-item outcomes (order may not match submission).</param>
/// <param name="ElapsedTime">Wall time for the whole bulk run.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsBulkSynthesizedEventArgs<TRequest>(IReadOnlyList<TtsResult<TRequest>> BulkTtsResults, TimeSpan ElapsedTime)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => $"Bulk TTS Results Count: {BulkTtsResults.Count}, Elapsed: {ElapsedTime:g}";
}