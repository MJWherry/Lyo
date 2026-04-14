using Lyo.Common.Enums;
using Lyo.Tts.Models;

namespace Lyo.Tts.WindowsSpeech;

public class WindowsTtsRequest:TtsRequest
{
    public string VoiceId { get; set; } = null!;

    public string Volume { get; set; }

    public string SpeechRate { get; set; }

    public AudioFormat? OutputFormat { get; set; }
}