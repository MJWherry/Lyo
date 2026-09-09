using Lyo.Exceptions;

namespace Lyo.Http.Client;

/// <summary>Optional delay before each send. Off for vendors; Flared turns this on instead of shipping a second sleep stack.</summary>
public sealed class LyoHttpTimingOptions
{
    /// <summary>When false, the timing handler is a no-op.</summary>
    public bool Enabled { get; set; }

    /// <summary>Inclusive minimum wait before send when enabled.</summary>
    public TimeSpan DelayBeforeSendMin { get; set; } = TimeSpan.Zero;

    /// <summary>Inclusive maximum wait before send when enabled.</summary>
    public TimeSpan DelayBeforeSendMax { get; set; } = TimeSpan.Zero;

    /// <summary>Extra ± fraction applied to the delay (0.2 = 20%). Also used on rate-limit waits when both handlers are on.</summary>
    public double Jitter { get; set; } = 0.2;

    /// <summary>Throws when max is less than min or jitter is negative.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIf(DelayBeforeSendMin < TimeSpan.Zero, "DelayBeforeSendMin must be >= 0.");
        ArgumentHelpers.ThrowIf(DelayBeforeSendMax < DelayBeforeSendMin, "DelayBeforeSendMax must be >= DelayBeforeSendMin.");
        ArgumentHelpers.ThrowIf(Jitter < 0, "Jitter must be >= 0.");
    }
}
