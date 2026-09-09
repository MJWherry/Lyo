namespace Lyo.Translation.Aws;

/// <summary>Shared constants for the AWS Translation library.</summary>
public static class Constants
{
    /// <summary>Metric name keys for AWS Translate instrumentation.</summary>
    public static class Metrics
    {
        /// <summary>AWS Translate namespaced alias of <see cref="Lyo.Translation.Constants.Metrics.TranslateDuration" />.</summary>
        public const string TranslateDuration = "translation.aws.translate.duration";

        /// <summary>Provider-scoped name for <see cref="Lyo.Translation.Constants.Metrics.TranslateSuccess" /> under AWS Translate.</summary>
        public const string TranslateSuccess = "translation.aws.translate.success";

        /// <summary>AWS Translate equivalent of <see cref="Lyo.Translation.Constants.Metrics.TranslateFailure" />.</summary>
        public const string TranslateFailure = "translation.aws.translate.failure";

        /// <summary>Same slot as <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateDuration" />, prefixed for AWS Translate.</summary>
        public const string BulkTranslateDuration = "translation.aws.bulk.translate.duration";

        /// <summary>AWS Translate copy of <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateTotal" />.</summary>
        public const string BulkTranslateTotal = "translation.aws.bulk.translate.total";

        /// <summary>Namespaced AWS Translate key for <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateSuccess" />.</summary>
        public const string BulkTranslateSuccess = "translation.aws.bulk.translate.success";

        /// <summary>AWS Translate stand-in for <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateFailure" />.</summary>
        public const string BulkTranslateFailure = "translation.aws.bulk.translate.failure";

        /// <summary>AWS Translate variant of <see cref="Lyo.Translation.Constants.Metrics.BulkTranslateLastDurationMs" />.</summary>
        public const string BulkTranslateLastDurationMs = "translation.aws.bulk.translate.last_duration_ms";

        /// <summary>AWS Translate-scoped match for <see cref="Lyo.Translation.Constants.Metrics.DetectLanguageDuration" />.</summary>
        public const string DetectLanguageDuration = "translation.aws.detectLanguage.duration";

        /// <summary>AWS Translate counterpart of <see cref="Lyo.Translation.Constants.Metrics.DetectLanguageSuccess" />.</summary>
        public const string DetectLanguageSuccess = "translation.aws.detectLanguage.success";

        /// <summary>AWS Translate-prefixed form of <see cref="Lyo.Translation.Constants.Metrics.DetectLanguageFailure" />.</summary>
        public const string DetectLanguageFailure = "translation.aws.detectLanguage.failure";
    }
}