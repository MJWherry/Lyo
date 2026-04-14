using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Enums;

namespace Lyo.Tts.Models;

/// <summary>Options for configuring a TTS service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TtsServiceOptions
{
    /// <summary>Gets or sets the default voice identifier to use when not specified in the request.</summary>
    public string? DefaultVoiceId { get; set; }

    /// <summary>Gets or sets the default output format (e.g., "mp3", "wav").</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AudioFormat? DefaultOutputFormat { get; set; }

    /// <summary>Gets or sets the maximum text length allowed for synthesis.</summary>
    public int MaxTextLength { get; set; } = 5000;

    /// <summary>Gets or sets whether to enable metrics tracking.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Gets or sets the concurrency limit for bulk synthesis operations.</summary>
    public int BulkTtsConcurrencyLimit { get; set; } = 10;

    /// <summary>Gets or sets the maximum number of requests allowed in a bulk operation.</summary>
    public int MaxBulkTtsLimit { get; set; } = 100;

    public override string ToString()
        => $"DefaultVoiceId={DefaultVoiceId}, DefaultOutputFormat={DefaultOutputFormat}, MaxTextLength={MaxTextLength}, EnableMetrics={EnableMetrics}, BulkTtsConcurrencyLimit={BulkTtsConcurrencyLimit}, MaxBulkTtsLimit={MaxBulkTtsLimit}";
}