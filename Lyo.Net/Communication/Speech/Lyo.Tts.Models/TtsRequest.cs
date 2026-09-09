using System.Text.Json.Serialization;
using Lyo.Common.Core.Enums;

namespace Lyo.Tts.Models;

/// <summary>Shared base for text-to-speech requests.</summary>
/// <remarks>
/// <para>Holds fields common to every TTS request. Derived types should expose typed enums that map onto these internal strings.</para>
/// <para>Internal properties are [JsonIgnore] so they are not serialized directly. Derived types own JSON through their own properties.</para>
/// </remarks>
public abstract class TtsRequest
{
    /// <summary>Text to speak.</summary>
    public string Text { get; set; } = null!;

    /// <summary>Voice id stored as a string.</summary>
    /// <remarks>Derived types should expose typed enums that map to this value.</remarks>
    [JsonIgnore]
    protected string? VoiceIdInternal { get; set; }

    /// <summary>Language code stored as a string.</summary>
    /// <remarks>Format is provider-specific (ISO 639-3 for Typecast, BCP 47 for AWS Polly). Derived types should expose typed enums that map to this value.</remarks>
    [JsonIgnore]
    protected string? LanguageInternal { get; set; }

    /// <summary>Audio format stored as a string.</summary>
    /// <remarks>Derived types should expose typed enums that map to this value.</remarks>
    [JsonIgnore]
    protected string? AudioFormatInternal { get; set; }

    /// <summary>Sex/gender stored as an enum.</summary>
    /// <remarks>Derived types should expose typed enums that map to this value.</remarks>
    [JsonIgnore]
    protected Sex? SexInternal { get; set; }
}