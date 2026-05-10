namespace Lyo.Tts.AwsPolly;

/// <summary>Consolidated constants for the AWS Polly TTS library.</summary>
public static class Constants
{
    /// <summary>Constants for AWS Polly TTS service metrics.</summary>
    public static class Metrics
    {
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.SynthesizeDuration" />.</summary>
        public const string SynthesizeDuration = "tts.awspolly.synthesize.duration";
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.SynthesizeSuccess" />.</summary>
        public const string SynthesizeSuccess = "tts.awspolly.synthesize.success";
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.SynthesizeFailure" />.</summary>
        public const string SynthesizeFailure = "tts.awspolly.synthesize.failure";
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeDuration" />.</summary>
        public const string BulkSynthesizeDuration = "tts.awspolly.bulk.synthesize.duration";
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeTotal" />.</summary>
        public const string BulkSynthesizeTotal = "tts.awspolly.bulk.synthesize.total";
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeSuccess" />.</summary>
        public const string BulkSynthesizeSuccess = "tts.awspolly.bulk.synthesize.success";
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeFailure" />.</summary>
        public const string BulkSynthesizeFailure = "tts.awspolly.bulk.synthesize.failure";
        /// <summary>AWS Polly–namespaced counterpart to <see cref="Lyo.Tts.Constants.Metrics.BulkSynthesizeLastDurationMs" />.</summary>
        public const string BulkSynthesizeLastDurationMs = "tts.awspolly.bulk.synthesize.last_duration_ms";
    }
}