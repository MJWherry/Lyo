using System.Text.Json.Serialization;
using Lyo.Common.Enums;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;

namespace Lyo.Stt.Models;

/// <summary>Options for configuring an STT service.</summary>
public class SttServiceOptions
{
    /// <summary>Gets or sets the default language code (e.g., "en-US").</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultLanguageCode { get; set; }

    /// <summary>Gets or sets the default audio format (e.g., "wav", "mp3").</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AudioFormat? DefaultAudioFormat { get; set; }

    /// <summary>Gets or sets the maximum audio file size in bytes.</summary>
    public long MaxAudioFileSize { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>Gets or sets whether to enable metrics tracking.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Gets or sets the concurrency limit for bulk recognition operations.</summary>
    public int BulkSttConcurrencyLimit { get; set; } = 10;

    /// <summary>Gets or sets the maximum number of requests allowed in a bulk operation.</summary>
    public int MaxBulkSttLimit { get; set; } = 100;
}