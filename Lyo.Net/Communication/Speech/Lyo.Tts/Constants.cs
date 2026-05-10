namespace Lyo.Tts;

/// <summary>Consolidated constants for the Tts library.</summary>
public static class Constants
{
    /// <summary>Constants for TTS service metric names and tags.</summary>
    public static class Metrics
    {
        /// <summary>Timer/histogram key for single-request synthesis latency.</summary>
        public const string SynthesizeDuration = "tts.synthesize.duration";

        /// <summary>Counter incremented when a synthesis call succeeds.</summary>
        public const string SynthesizeSuccess = "tts.synthesize.success";

        /// <summary>Counter incremented when a synthesis call fails.</summary>
        public const string SynthesizeFailure = "tts.synthesize.failure";

        /// <summary>Timer/histogram key for an entire bulk synthesis batch.</summary>
        public const string BulkSynthesizeDuration = "tts.bulk.synthesize.duration";

        /// <summary>Counter incremented by the number of requests in each bulk batch.</summary>
        public const string BulkSynthesizeTotal = "tts.bulk.synthesize.total";

        /// <summary>Counter incremented by successful items in a bulk batch.</summary>
        public const string BulkSynthesizeSuccess = "tts.bulk.synthesize.success";

        /// <summary>Counter incremented by failed items in a bulk batch.</summary>
        public const string BulkSynthesizeFailure = "tts.bulk.synthesize.failure";

        /// <summary>Gauge storing the duration in milliseconds of the last completed bulk batch.</summary>
        public const string BulkSynthesizeLastDurationMs = "tts.bulk.synthesize.last_duration_ms";
    }
}