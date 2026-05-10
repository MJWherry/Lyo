namespace Lyo.Translation;

/// <summary>Consolidated constants for the Translation library.</summary>
public static class Constants
{
    /// <summary>Constants for translation service metrics.</summary>
    public static class Metrics
    {
        /// <summary>Timer/histogram key for single-request translation latency.</summary>
        public const string TranslateDuration = "translation.Service.translate.duration";
        /// <summary>Counter for successful translate calls.</summary>
        public const string TranslateSuccess = "translation.Service.translate.success";
        /// <summary>Counter for failed translate calls.</summary>
        public const string TranslateFailure = "translation.Service.translate.failure";
        /// <summary>Timer/histogram for a full bulk translate batch.</summary>
        public const string BulkTranslateDuration = "translation.Service.bulk.translate.duration";
        /// <summary>Counter incremented by batched request count.</summary>
        public const string BulkTranslateTotal = "translation.Service.bulk.translate.total";
        /// <summary>Counter for successful bulk items.</summary>
        public const string BulkTranslateSuccess = "translation.Service.bulk.translate.success";
        /// <summary>Counter for failed bulk items.</summary>
        public const string BulkTranslateFailure = "translation.Service.bulk.translate.failure";
        /// <summary>Gauge for last bulk batch duration in milliseconds.</summary>
        public const string BulkTranslateLastDurationMs = "translation.Service.bulk.translate.last_duration_ms";
        /// <summary>Timer/histogram for language detection.</summary>
        public const string DetectLanguageDuration = "translation.Service.detectLanguage.duration";
        /// <summary>Counter for successful detection.</summary>
        public const string DetectLanguageSuccess = "translation.Service.detectLanguage.success";
        /// <summary>Counter for failed detection.</summary>
        public const string DetectLanguageFailure = "translation.Service.detectLanguage.failure";
    }
}