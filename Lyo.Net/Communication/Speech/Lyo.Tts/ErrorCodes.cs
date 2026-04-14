namespace Lyo.Tts;

/// <summary>Error codes used by TTS services.</summary>
public static class TtsErrorCodes
{
    /// <summary>Failed to synthesize text to speech.</summary>
    public const string SynthesizeFailed = "TTS_SYNTHESIZE_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "TTS_OPERATION_CANCELLED";

    /// <summary>Failed to write audio data to file.</summary>
    public const string FileWriteFailed = "TTS_FILE_WRITE_FAILED";

    /// <summary>Failed to write audio data to stream.</summary>
    public const string StreamWriteFailed = "TTS_STREAM_WRITE_FAILED";
}