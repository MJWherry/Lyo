namespace Lyo.Tts;

/// <summary>Stable error codes emitted by TTS services.</summary>
public static class TtsErrorCodes
{
    /// <summary>Speech synthesis did not complete.</summary>
    public const string SynthesizeFailed = "TTS_SYNTHESIZE_FAILED";

    /// <summary>The operation was cancelled before it finished.</summary>
    public const string OperationCancelled = "TTS_OPERATION_CANCELLED";

    /// <summary>Audio could not be written to a file.</summary>
    public const string FileWriteFailed = "TTS_FILE_WRITE_FAILED";

    /// <summary>Audio could not be written to a stream.</summary>
    public const string StreamWriteFailed = "TTS_STREAM_WRITE_FAILED";
}