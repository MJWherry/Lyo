namespace Lyo.Translation;

/// <summary>Error codes used by Translation services.</summary>
public static class TranslationErrorCodes
{
    /// <summary>Failed to translate text.</summary>
    public const string TranslateFailed = "TRANSLATION_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "TRANSLATION_OPERATION_CANCELLED";

    /// <summary>Failed to detect language.</summary>
    public const string DetectLanguageFailed = "TRANSLATION_DETECT_LANGUAGE_FAILED";
}