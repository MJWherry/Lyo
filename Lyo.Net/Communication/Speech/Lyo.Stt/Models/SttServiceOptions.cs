using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;

namespace Lyo.Stt.Models;

/// <summary>Settings that configure an STT service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SttServiceOptions
{
    /// <summary>Default language (for example "en-US").</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultLanguageCode { get; set; }

    /// <summary>Default audio format (for example "wav" or "mp3").</summary>
    public AudioFormat? DefaultAudioFormat { get; set; }

    /// <summary>Largest accepted audio file, in bytes.</summary>
    public long MaxAudioFileSize { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>Whether metrics are recorded.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>How many bulk recognitions may run at once.</summary>
    public int BulkSttConcurrencyLimit { get; set; } = 10;

    /// <summary>Upper bound on requests in one bulk call.</summary>
    public int MaxBulkSttLimit { get; set; } = 100;

    /// <summary>Language, audio limits, and bulk caps.</summary>
    public override string ToString()
        => $"DefaultLanguageCode={DefaultLanguageCode} DefaultAudioFormat={DefaultAudioFormat} MaxAudioFileSize={MaxAudioFileSize} EnableMetrics={EnableMetrics} BulkSttConcurrencyLimit={BulkSttConcurrencyLimit} MaxBulkSttLimit={MaxBulkSttLimit}";
}