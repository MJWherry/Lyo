namespace Lyo.Translation.Aws;

/// <summary>Consolidated constants for the AWS Translation library.</summary>
public static class Constants
{
    /// <summary>Constants for AWS Translate service metrics.</summary>
    public static class Metrics
    {
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.TranslateDuration" />.</summary>
        public const string TranslateDuration = "translation.aws.translate.duration";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.TranslateSuccess" />.</summary>
        public const string TranslateSuccess = "translation.aws.translate.success";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.TranslateFailure" />.</summary>
        public const string TranslateFailure = "translation.aws.translate.failure";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateDuration" />.</summary>
        public const string BulkTranslateDuration = "translation.aws.bulk.translate.duration";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateTotal" />.</summary>
        public const string BulkTranslateTotal = "translation.aws.bulk.translate.total";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateSuccess" />.</summary>
        public const string BulkTranslateSuccess = "translation.aws.bulk.translate.success";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateFailure" />.</summary>
        public const string BulkTranslateFailure = "translation.aws.bulk.translate.failure";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateLastDurationMs" />.</summary>
        public const string BulkTranslateLastDurationMs = "translation.aws.bulk.translate.last_duration_ms";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.DetectLanguageDuration" />.</summary>
        public const string DetectLanguageDuration = "translation.aws.detectLanguage.duration";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.DetectLanguageSuccess" />.</summary>
        public const string DetectLanguageSuccess = "translation.aws.detectLanguage.success";
        /// <summary>AWS Translate–namespaced counterpart to <see cref="Lyo.Translation.Constants.Metrics.DetectLanguageFailure" />.</summary>
        public const string DetectLanguageFailure = "translation.aws.detectLanguage.failure";
    }
}