namespace Lyo.Tts.AwsPolly;

/// <summary>Shared constants for the AWS Polly TTS library.</summary>
public static class Constants
{
    /// <summary>Metric name keys for AWS Polly TTS instrumentation.</summary>
    public static class Metrics
    {
        /// <summary>AWS Polly namespaced alias of <see cref="Lyo.Tts.Constants.Metrics.SynthesizeDuration" />.</summary>
        public const string SynthesizeDuration = "tts.awspolly.synthesize.duration";

        /// <summary>Provider-scoped name for <see cref="Lyo.Tts.Constants.Metrics.SynthesizeSuccess" /> under AWS Polly.</summary>
        public const string SynthesizeSuccess = "tts.awspolly.synthesize.success";

        /// <summary>AWS Polly equivalent of <see cref="Lyo.Tts.Constants.Metrics.SynthesizeFailure" />.</summary>
        public const string SynthesizeFailure = "tts.awspolly.synthesize.failure";

        /// <summary>Same slot as <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeDuration" />, prefixed for AWS Polly.</summary>
        public const string BulkSynthesizeDuration = "tts.awspolly.bulk.synthesize.duration";

        /// <summary>AWS Polly copy of <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeTotal" />.</summary>
        public const string BulkSynthesizeTotal = "tts.awspolly.bulk.synthesize.total";

        /// <summary>Namespaced AWS Polly key for <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeSuccess" />.</summary>
        public const string BulkSynthesizeSuccess = "tts.awspolly.bulk.synthesize.success";

        /// <summary>AWS Polly stand-in for <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeFailure" />.</summary>
        public const string BulkSynthesizeFailure = "tts.awspolly.bulk.synthesize.failure";

        /// <summary>AWS Polly variant of <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeLastDurationMs" />.</summary>
        public const string BulkSynthesizeLastDurationMs = "tts.awspolly.bulk.synthesize.last_duration_ms";
    }
}