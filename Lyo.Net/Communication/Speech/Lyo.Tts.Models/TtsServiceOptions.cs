using System.Diagnostics;
using Lyo.Common.Core.Enums;

namespace Lyo.Tts.Models;

/// <summary>Settings that configure a TTS service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TtsServiceOptions
{
    /// <summary>Voice used when a request does not name one.</summary>
    public string? DefaultVoiceId { get; set; }

    /// <summary>Default output format (for example "mp3" or "wav").</summary>
    public AudioFormat? DefaultOutputFormat { get; set; }

    /// <summary>Longest text accepted for synthesis.</summary>
    public int MaxTextLength { get; set; } = 5000;

    /// <summary>Whether metrics are recorded.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>How many bulk syntheses may run at once.</summary>
    public int BulkTtsConcurrencyLimit { get; set; } = 10;

    /// <summary>Upper bound on requests in one bulk call.</summary>
    public int MaxBulkTtsLimit { get; set; } = 100;

    /// <inheritdoc />
    public override string ToString()
        => $"DefaultVoiceId={DefaultVoiceId}, DefaultOutputFormat={DefaultOutputFormat}, MaxTextLength={MaxTextLength}, EnableMetrics={EnableMetrics}, BulkTtsConcurrencyLimit={BulkTtsConcurrencyLimit}, MaxBulkTtsLimit={MaxBulkTtsLimit}";
}