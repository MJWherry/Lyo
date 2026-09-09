namespace Lyo.Tts;

/// <summary>Shared constants used throughout the Tts library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags for TTS service instrumentation.</summary>
    public static class Metrics
    {
        /// <summary>Timer/histogram name for latency of one synthesis call.</summary>
        public const string SynthesizeDuration = "tts.synthesize.duration";

        /// <summary>Counts synthesis calls that succeed.</summary>
        public const string SynthesizeSuccess = "tts.synthesize.success";

        /// <summary>Counts synthesis calls that fail.</summary>
        public const string SynthesizeFailure = "tts.synthesize.failure";

        /// <summary>Timer/histogram covering an entire bulk synthesis batch.</summary>
        public const string BulkSynthesizeDuration = "tts.bulk.synthesize.duration";

        /// <summary>Increments by the number of requests in the batch.</summary>
        public const string BulkSynthesizeTotal = "tts.bulk.synthesize.total";

        /// <summary>Counts bulk items that succeed.</summary>
        public const string BulkSynthesizeSuccess = "tts.bulk.synthesize.success";

        /// <summary>Counts bulk items that fail.</summary>
        public const string BulkSynthesizeFailure = "tts.bulk.synthesize.failure";

        /// <summary>Gauge holding the most recent bulk batch duration, in milliseconds.</summary>
        public const string BulkSynthesizeLastDurationMs = "tts.bulk.synthesize.last_duration_ms";
    }
}