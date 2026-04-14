using System.Text.Json.Serialization;

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Emotion and style settings for speech generation (ssfm-v30).</summary>
public class Prompt
{
    /// <summary>Emotion type. For ssfm-v30: "smart" (uses previous_text and next_text) or preset emotion names.</summary>
    [JsonPropertyName("emotion_type")]
    public string? EmotionType { get; set; }

    /// <summary>Previous text context for smart emotion detection.</summary>
    [JsonPropertyName("previous_text")]
    public string? PreviousText { get; set; }

    /// <summary>Next text context for smart emotion detection.</summary>
    [JsonPropertyName("next_text")]
    public string? NextText { get; set; }
}