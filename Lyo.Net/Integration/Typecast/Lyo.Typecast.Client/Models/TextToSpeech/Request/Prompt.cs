using System.Text.Json.Serialization;

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Emotion/style settings for speech generation (ssfm-v30).</summary>
public class Prompt
{
    /// <summary>Emotion kind. For ssfm-v30: "smart" (uses previous_text and next_text) or preset emotion names.</summary>
    [JsonPropertyName("emotion_type")]
    public string? EmotionType { get; set; }

    /// <summary>Smart emotion detection previous text context.</summary>
    [JsonPropertyName("previous_text")]
    public string? PreviousText { get; set; }

    /// <summary>Smart emotion detection next text context.</summary>
    [JsonPropertyName("next_text")]
    public string? NextText { get; set; }
}