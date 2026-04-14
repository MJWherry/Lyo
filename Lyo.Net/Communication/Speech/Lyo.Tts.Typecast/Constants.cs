namespace Lyo.Tts.Typecast;

/// <summary>Consolidated constants for the Typecast TTS library.</summary>
public static class Constants
{
    /// <summary>Constants for Typecast TTS service metrics.</summary>
    public static class Metrics
    {
        public const string SynthesizeDuration = "tts.typecast.synthesize.duration";
        public const string SynthesizeSuccess = "tts.typecast.synthesize.success";
        public const string SynthesizeFailure = "tts.typecast.synthesize.failure";
        public const string BulkSynthesizeDuration = "tts.typecast.bulk.synthesize.duration";
        public const string BulkSynthesizeTotal = "tts.typecast.bulk.synthesize.total";
        public const string BulkSynthesizeSuccess = "tts.typecast.bulk.synthesize.success";
        public const string BulkSynthesizeFailure = "tts.typecast.bulk.synthesize.failure";
        public const string BulkSynthesizeLastDurationMs = "tts.typecast.bulk.synthesize.last_duration_ms";
    }
}