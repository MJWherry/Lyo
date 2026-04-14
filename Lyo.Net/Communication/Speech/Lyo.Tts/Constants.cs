namespace Lyo.Tts;

/// <summary>Consolidated constants for the Tts library.</summary>
public static class Constants
{
    /// <summary>Constants for TTS service metric names and tags.</summary>
    public static class Metrics
    {
        public const string SynthesizeDuration = "tts.synthesize.duration";

        public const string SynthesizeSuccess = "tts.synthesize.success";

        public const string SynthesizeFailure = "tts.synthesize.failure";

        public const string BulkSynthesizeDuration = "tts.bulk.synthesize.duration";

        public const string BulkSynthesizeTotal = "tts.bulk.synthesize.total";

        public const string BulkSynthesizeSuccess = "tts.bulk.synthesize.success";

        public const string BulkSynthesizeFailure = "tts.bulk.synthesize.failure";

        public const string BulkSynthesizeLastDurationMs = "tts.bulk.synthesize.last_duration_ms";
    }
}