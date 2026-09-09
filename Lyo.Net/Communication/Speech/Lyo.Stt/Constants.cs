namespace Lyo.Stt;

/// <summary>Shared constants used throughout the Stt library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags for STT service instrumentation.</summary>
    public static class Metrics
    {
        public const string RecognizeDuration = "stt.recognize.duration";

        public const string RecognizeSuccess = "stt.recognize.success";

        public const string RecognizeFailure = "stt.recognize.failure";

        public const string BulkRecognizeDuration = "stt.bulk.recognize.duration";

        public const string BulkRecognizeTotal = "stt.bulk.recognize.total";

        public const string BulkRecognizeSuccess = "stt.bulk.recognize.success";

        public const string BulkRecognizeFailure = "stt.bulk.recognize.failure";

        public const string BulkRecognizeLastDurationMs = "stt.bulk.recognize.last_duration_ms";
    }
}