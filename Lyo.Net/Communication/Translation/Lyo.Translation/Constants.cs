namespace Lyo.Translation;

/// <summary>Shared constants used throughout the Translation library.</summary>
public static class Constants
{
    /// <summary>Metric name keys for translation service instrumentation.</summary>
    public static class Metrics
    {
        /// <summary>Timer/histogram name for latency of one translate call.</summary>
        public const string TranslateDuration = "translation.Service.translate.duration";

        /// <summary>Counts translate calls that succeed.</summary>
        public const string TranslateSuccess = "translation.Service.translate.success";

        /// <summary>Counts translate calls that fail.</summary>
        public const string TranslateFailure = "translation.Service.translate.failure";

        /// <summary>Timer/histogram covering an entire bulk translate batch.</summary>
        public const string BulkTranslateDuration = "translation.Service.bulk.translate.duration";

        /// <summary>Increments by the number of requests in the batch.</summary>
        public const string BulkTranslateTotal = "translation.Service.bulk.translate.total";

        /// <summary>Counts bulk items that succeed.</summary>
        public const string BulkTranslateSuccess = "translation.Service.bulk.translate.success";

        /// <summary>Counts bulk items that fail.</summary>
        public const string BulkTranslateFailure = "translation.Service.bulk.translate.failure";

        /// <summary>Gauge holding the most recent bulk batch duration, in milliseconds.</summary>
        public const string BulkTranslateLastDurationMs = "translation.Service.bulk.translate.last_duration_ms";

        /// <summary>Timer/histogram for language-detection latency.</summary>
        public const string DetectLanguageDuration = "translation.Service.detectLanguage.duration";

        /// <summary>Counts language detections that succeed.</summary>
        public const string DetectLanguageSuccess = "translation.Service.detectLanguage.success";

        /// <summary>Counts language detections that fail.</summary>
        public const string DetectLanguageFailure = "translation.Service.detectLanguage.failure";
    }
}