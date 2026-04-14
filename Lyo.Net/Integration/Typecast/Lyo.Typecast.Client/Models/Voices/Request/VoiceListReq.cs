using System.Diagnostics;
using Lyo.Typecast.Client.Enums;

namespace Lyo.Typecast.Client.Models.Voices.Request;

/// <summary>Request model for listing Typecast voices with optional filters.</summary>
[DebuggerDisplay("{ToString(), nq}")]
public class VoiceListReq
{
    /// <summary>Filter by model version.</summary>
    //[JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>Filter by gender.</summary>
    //[JsonPropertyName("gender")]
    public Gender? Gender { get; set; }

    /// <summary>Filter by age group.</summary>
    //[JsonPropertyName("age")]
    public AgeGroup? Age { get; set; }

    /// <summary>Filter by use cases (e.g., "Conversational", "TikTok/Reels/Shorts", "Audiobook/Storytelling").</summary>
    //[JsonPropertyName("use_cases")]
    public List<string>? UseCases { get; set; }

    public override string ToString() => $"Model={Model} Gender={Gender} Age={Age} UseCases=[{(UseCases == null ? "" : string.Join(",", UseCases))}]";
}