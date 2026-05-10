using System.Diagnostics;

namespace Lyo.Tts.Models;

/// <summary>Payload for the Synthesizing event raised by <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="TtsRequest">The request about to be processed.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsSynthesizingEventArgs<TRequest>(TRequest TtsRequest)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => TtsRequest.ToString()!;
}

/// <summary>Payload for the BulkSynthesizing event raised by <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="BulkTtsRequests">All items in the current bulk operation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsBulkSynthesizingEventArgs<TRequest>(IReadOnlyList<TRequest> BulkTtsRequests)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => "Bulk TTS Requests Count: " + BulkTtsRequests.Count;
}

/// <summary>Payload for the Synthesized event raised by <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="TtsResult">The outcome of a single synthesis.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsSynthesizedEventArgs<TRequest>(TtsResult<TRequest> TtsResult)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => TtsResult.ToString();
}

/// <summary>Payload for the BulkSynthesized event raised by <c>TtsServiceBase&lt;TRequest&gt;</c>.</summary>
/// <param name="BulkTtsResults">Per-item outcomes (order not guaranteed to match submission order).</param>
/// <param name="ElapsedTime">Wall-clock time for the full bulk operation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TtsBulkSynthesizedEventArgs<TRequest>(IReadOnlyList<TtsResult<TRequest>> BulkTtsResults, TimeSpan ElapsedTime)
    where TRequest : TtsRequest
{
    /// <inheritdoc />
    public override string ToString() => $"Bulk TTS Results Count: {BulkTtsResults.Count}, Elapsed: {ElapsedTime:g}";
}