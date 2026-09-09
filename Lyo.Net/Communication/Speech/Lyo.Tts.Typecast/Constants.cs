namespace Lyo.Tts.Typecast;

/// <summary>Shared constants for the Typecast TTS library.</summary>
public static class Constants
{
    /// <summary>Metric name keys for Typecast TTS instrumentation.</summary>
    public static class Metrics
    {
        /// <summary>Typecast namespaced alias of <see cref="Lyo.Tts.Constants.Metrics.SynthesizeDuration" />.</summary>
        public const string SynthesizeDuration = "tts.typecast.synthesize.duration";

        /// <summary>Provider-scoped name for <see cref="Lyo.Tts.Constants.Metrics.SynthesizeSuccess" /> under Typecast.</summary>
        public const string SynthesizeSuccess = "tts.typecast.synthesize.success";

        /// <summary>Typecast equivalent of <see cref="Lyo.Tts.Constants.Metrics.SynthesizeFailure" />.</summary>
        public const string SynthesizeFailure = "tts.typecast.synthesize.failure";

        /// <summary>Same slot as <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeDuration" />, prefixed for Typecast.</summary>
        public const string BulkSynthesizeDuration = "tts.typecast.bulk.synthesize.duration";

        /// <summary>Typecast copy of <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeTotal" />.</summary>
        public const string BulkSynthesizeTotal = "tts.typecast.bulk.synthesize.total";

        /// <summary>Namespaced Typecast key for <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeSuccess" />.</summary>
        public const string BulkSynthesizeSuccess = "tts.typecast.bulk.synthesize.success";

        /// <summary>Typecast stand-in for <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeFailure" />.</summary>
        public const string BulkSynthesizeFailure = "tts.typecast.bulk.synthesize.failure";

        /// <summary>Typecast variant of <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeLastDurationMs" />.</summary>
        public const string BulkSynthesizeLastDurationMs = "tts.typecast.bulk.synthesize.last_duration_ms";
    }
}