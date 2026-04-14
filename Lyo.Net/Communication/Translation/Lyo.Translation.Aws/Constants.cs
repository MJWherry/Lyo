namespace Lyo.Translation.Aws;

/// <summary>Consolidated constants for the AWS Translation library.</summary>
public static class Constants
{
    /// <summary>Constants for AWS Translate service metrics.</summary>
    public static class Metrics
    {
        public const string TranslateDuration = "translation.aws.translate.duration";
        public const string TranslateSuccess = "translation.aws.translate.success";
        public const string TranslateFailure = "translation.aws.translate.failure";
        public const string BulkTranslateDuration = "translation.aws.bulk.translate.duration";
        public const string BulkTranslateTotal = "translation.aws.bulk.translate.total";
        public const string BulkTranslateSuccess = "translation.aws.bulk.translate.success";
        public const string BulkTranslateFailure = "translation.aws.bulk.translate.failure";
        public const string BulkTranslateLastDurationMs = "translation.aws.bulk.translate.last_duration_ms";
        public const string DetectLanguageDuration = "translation.aws.detectLanguage.duration";
        public const string DetectLanguageSuccess = "translation.aws.detectLanguage.success";
        public const string DetectLanguageFailure = "translation.aws.detectLanguage.failure";
    }
}