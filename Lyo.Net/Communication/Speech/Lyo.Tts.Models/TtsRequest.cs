using System.Text.Json.Serialization;
using Lyo.Common.Enums;

namespace Lyo.Tts.Models;

/// <summary>Base class for text-to-speech requests.</summary>
/// <remarks>
/// <para>This base class provides common properties for all TTS requests. Derived classes should expose typed enum properties that map to these internal string properties.</para>
/// <para>The internal properties are marked with [JsonIgnore] to prevent direct serialization. Derived classes control JSON serialization through their own properties.</para>
/// </remarks>
public abstract class TtsRequest
{
    /// <summary>Gets or sets the text to synthesize into speech.</summary>
    public string Text { get; set; } = null!;

    /// <summary>Gets or sets the voice identifier as a string (internal storage).</summary>
    /// <remarks>Derived classes should expose typed enum properties that map to this value.</remarks>
    [JsonIgnore]
    protected string? VoiceIdInternal { get; set; }

    /// <summary>Gets or sets the language code as a string (internal storage).</summary>
    /// <remarks>The format depends on the implementation (e.g., ISO 639-3 for Typecast, BCP 47 for AWS Polly). Derived classes should expose typed enum properties that map to this value.</remarks>
    [JsonIgnore]
    protected string? LanguageInternal { get; set; }

    /// <summary>Gets or sets the audio format as a string (internal storage).</summary>
    /// <remarks>Derived classes should expose typed enum properties that map to this value.</remarks>
    [JsonIgnore]
    protected string? AudioFormatInternal { get; set; }

    /// <summary>Gets or sets the sex/gender as an enum (internal storage).</summary>
    /// <remarks>Derived classes should expose typed enum properties that map to this value.</remarks>
    [JsonIgnore]
    protected Sex? SexInternal { get; set; }
}