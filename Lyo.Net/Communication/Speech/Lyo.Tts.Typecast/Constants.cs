namespace Lyo.Tts.Typecast;

/// <summary>Consolidated constants for the Typecast TTS library.</summary>
public static class Constants
{
    /// <summary>Constants for Typecast TTS service metrics.</summary>
    public static class Metrics
    {
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.SynthesizeDuration" />.</summary>
        public const string SynthesizeDuration = "tts.typecast.synthesize.duration";
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.SynthesizeSuccess" />.</summary>
        public const string SynthesizeSuccess = "tts.typecast.synthesize.success";
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.SynthesizeFailure" />.</summary>
        public const string SynthesizeFailure = "tts.typecast.synthesize.failure";
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeDuration" />.</summary>
        public const string BulkSynthesizeDuration = "tts.typecast.bulk.synthesize.duration";
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeTotal" />.</summary>
        public const string BulkSynthesizeTotal = "tts.typecast.bulk.synthesize.total";
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeSuccess" />.</summary>
        public const string BulkSynthesizeSuccess = "tts.typecast.bulk.synthesize.success";
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeFailure" />.</summary>
        public const string BulkSynthesizeFailure = "tts.typecast.bulk.synthesize.failure";
        /// <summary>Typecast–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeLastDurationMs" />.</summary>
        public const string BulkSynthesizeLastDurationMs = "tts.typecast.bulk.synthesize.last_duration_ms";
    }
}