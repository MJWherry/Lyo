using System.Diagnostics;
using Lyo.Typecast.Client.Enums;

namespace Lyo.Typecast.Client.Models.Voices.Response;

/// <summary>Represents a Typecast voice with its characteristics and supported models.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Voice(string VoiceId, string VoiceName, IReadOnlyList<VoiceModel> Models, Gender? Gender, AgeGroup? Age, IReadOnlyList<string> UseCases)
{
    public override string ToString()
        => $"VoiceId: {VoiceId}, VoiceName: {VoiceName} Gender: {Gender}, Age: {Age}, UseCases: [{string.Join(",", UseCases)}], Models: [{string.Join(" | ", Models)}]";
}