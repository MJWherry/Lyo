using System.Diagnostics;

namespace Lyo.Typecast.Client.Models.Voices.Response;

/// <summary>Represents a voice model with its version and supported emotions.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record VoiceModel(string Version, IReadOnlyList<string> Emotions)
{
    public override string ToString() => $"Version: {Version}, Emotions: [{string.Join(",", Emotions)}]";
}