using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TimeInterval(double seconds)
{
    public double Seconds { get; } = MathValueGuards.NonNegativeFinite(seconds, nameof(seconds));

    public double Minutes => Seconds / 60d;

    public double Hours => Seconds / 3600d;

    public TimeSpan TimeSpan => TimeSpan.FromSeconds(Seconds);

    public static TimeInterval FromSeconds(double seconds) => new(seconds);

    public static TimeInterval FromMinutes(double minutes) => new(MathValueGuards.NonNegativeFinite(minutes, nameof(minutes)) * 60d);

    public static TimeInterval FromHours(double hours) => new(MathValueGuards.NonNegativeFinite(hours, nameof(hours)) * 3600d);

    public override string ToString() => $"{Seconds:0.###} s";
}