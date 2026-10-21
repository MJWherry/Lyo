namespace Lyo.Media.Models;

/// <summary>EBU loudness target for a two-pass <c>loudnorm</c> measure and apply. Mutually exclusive with a raw audio filter.</summary>
public sealed record LoudnessTarget
{
    /// <summary>Streaming target, −16 LUFS, −1.5 dBTP, LRA 11.</summary>
    public static LoudnessTarget Streaming { get; } = new();

    /// <summary>EBU R128 broadcast target, −23 LUFS, −1 dBTP, LRA 7.</summary>
    public static LoudnessTarget Broadcast { get; } = new() { IntegratedLufs = -23, TruePeak = -1, Range = 7 };

    /// <summary>Integrated loudness in LUFS. Starts at −16.</summary>
    public double IntegratedLufs { get; init; } = -16;

    /// <summary>True peak in dBTP. Starts at −1.5.</summary>
    public double TruePeak { get; init; } = -1.5;

    /// <summary>Loudness range. Starts at 11.</summary>
    public double Range { get; init; } = 11;
}