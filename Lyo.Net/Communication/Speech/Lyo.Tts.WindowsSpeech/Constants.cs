#if WINDOWS || NETFRAMEWORK
namespace Lyo.Tts.WindowsSpeech;

/// <summary>Consolidated constants for the Windows Speech TTS library.</summary>
public static class Constants
{
    /// <summary>Constants for Windows Speech TTS service metrics.</summary>
    public static class Metrics
    {
        public const string SynthesizeDuration = "tts.windowsspeech.synthesize.duration";
        public const string SynthesizeSuccess = "tts.windowsspeech.synthesize.success";
        public const string SynthesizeFailure = "tts.windowsspeech.synthesize.failure";
        public const string BulkSynthesizeDuration = "tts.windowsspeech.bulk.synthesize.duration";
        public const string BulkSynthesizeTotal = "tts.windowsspeech.bulk.synthesize.total";
        public const string BulkSynthesizeSuccess = "tts.windowsspeech.bulk.synthesize.success";
        public const string BulkSynthesizeFailure = "tts.windowsspeech.bulk.synthesize.failure";
        public const string BulkSynthesizeLastDurationMs = "tts.windowsspeech.bulk.synthesize.last_duration_ms";
    }
}
#endif
