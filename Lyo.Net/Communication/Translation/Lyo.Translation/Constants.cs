namespace Lyo.Translation;

/// <summary>Consolidated constants for the Translation library.</summary>
public static class Constants
{
    /// <summary>Constants for translation service metrics.</summary>
    public static class Metrics
    {
        public const string TranslateDuration = "translation.Service.translate.duration";
        public const string TranslateSuccess = "translation.Service.translate.success";
        public const string TranslateFailure = "translation.Service.translate.failure";
        public const string BulkTranslateDuration = "translation.Service.bulk.translate.duration";
        public const string BulkTranslateTotal = "translation.Service.bulk.translate.total";
        public const string BulkTranslateSuccess = "translation.Service.bulk.translate.success";
        public const string BulkTranslateFailure = "translation.Service.bulk.translate.failure";
        public const string BulkTranslateLastDurationMs = "translation.Service.bulk.translate.last_duration_ms";
        public const string DetectLanguageDuration = "translation.Service.detectLanguage.duration";
        public const string DetectLanguageSuccess = "translation.Service.detectLanguage.success";
        public const string DetectLanguageFailure = "translation.Service.detectLanguage.failure";
    }
}