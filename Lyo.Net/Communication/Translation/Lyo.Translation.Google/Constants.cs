namespace Lyo.Translation.Google;

/// <summary>Consolidated constants for the Google Translation library.</summary>
public static class Constants
{
    /// <summary>Constants for Google Translate service metrics.</summary>
    public static class Metrics
    {
        public const string TranslateDuration = "translation.google.translate.duration";
        public const string TranslateSuccess = "translation.google.translate.success";
        public const string TranslateFailure = "translation.google.translate.failure";
        public const string BulkTranslateDuration = "translation.google.bulk.translate.duration";
        public const string BulkTranslateTotal = "translation.google.bulk.translate.total";
        public const string BulkTranslateSuccess = "translation.google.bulk.translate.success";
        public const string BulkTranslateFailure = "translation.google.bulk.translate.failure";
        public const string BulkTranslateLastDurationMs = "translation.google.bulk.translate.last_duration_ms";
        public const string DetectLanguageDuration = "translation.google.detectLanguage.duration";
        public const string DetectLanguageSuccess = "translation.google.detectLanguage.success";
        public const string DetectLanguageFailure = "translation.google.detectLanguage.failure";
    }
}