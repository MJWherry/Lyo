namespace Lyo.Tts.AwsPolly;

/// <summary>Consolidated constants for the AWS Polly TTS library.</summary>
public static class Constants
{
    /// <summary>Constants for AWS Polly TTS service metrics.</summary>
    public static class Metrics
    {
        public const string SynthesizeDuration = "tts.awspolly.synthesize.duration";
        public const string SynthesizeSuccess = "tts.awspolly.synthesize.success";
        public const string SynthesizeFailure = "tts.awspolly.synthesize.failure";
        public const string BulkSynthesizeDuration = "tts.awspolly.bulk.synthesize.duration";
        public const string BulkSynthesizeTotal = "tts.awspolly.bulk.synthesize.total";
        public const string BulkSynthesizeSuccess = "tts.awspolly.bulk.synthesize.success";
        public const string BulkSynthesizeFailure = "tts.awspolly.bulk.synthesize.failure";
        public const string BulkSynthesizeLastDurationMs = "tts.awspolly.bulk.synthesize.last_duration_ms";
    }
}