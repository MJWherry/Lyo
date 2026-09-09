namespace Lyo.Translation;

/// <summary>Stable error codes emitted by Translation services.</summary>
public static class TranslationErrorCodes
{
    /// <summary>Text translation did not complete.</summary>
    public const string TranslateFailed = "TRANSLATION_FAILED";

    /// <summary>The operation was cancelled before it finished.</summary>
    public const string OperationCancelled = "TRANSLATION_OPERATION_CANCELLED";

    /// <summary>Language detection did not complete.</summary>
    public const string DetectLanguageFailed = "TRANSLATION_DETECT_LANGUAGE_FAILED";
}