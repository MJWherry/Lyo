namespace Lyo.Web.Automation.Models;

/// <summary>
/// A desktop width×height in CSS pixels. Used for session start and the <c>randomizeViewport</c> plan step.
/// Headless Chromium reports <c>window.screen</c> as 800×600 unless the engine sets a real screen size — that pair is a bot-detection tell, so it is not in
/// <see cref="Common" />.
/// </summary>
public readonly record struct DesktopDisplaySize(int Width, int Height)
{
    /// <summary>Lower bound after jitter so a roll cannot land on Chromium's 800×600 headless default.</summary>
    public const int MinWidth = 1024;

    /// <summary>Lower bound after jitter so a roll cannot land on Chromium's 800×600 headless default.</summary>
    public const int MinHeight = 720;

    /// <summary>Common maximized / laptop sizes. Prefer these over arbitrary pixel jitter.</summary>
    public static IReadOnlyList<DesktopDisplaySize> Common { get; } = [
        new(1920, 1080),
        new(1920, 1200),
        new(1680, 1050),
        new(1600, 900),
        new(1536, 864),
        new(1440, 900),
        new(1366, 768),
        new(2560, 1440)
    ];

    /// <summary>
    /// Picks one size from <paramref name="sizes" /> (or <see cref="Common" /> when null/empty), then optionally adds ±<paramref name="jitterPixels" /> on each axis, clamped
    /// to <see cref="MinWidth" />×<see cref="MinHeight" />.
    /// </summary>
    public static DesktopDisplaySize Pick(IReadOnlyList<DesktopDisplaySize>? sizes, int jitterPixels, Random? random = null)
    {
        var pool = sizes is { Count: > 0 } ? sizes : Common;
        var rng = random ?? new Random();
        var pick = pool[rng.Next(pool.Count)];
        if (jitterPixels <= 0)
            return pick;

        var width = Math.Max(MinWidth, pick.Width + rng.Next(-jitterPixels, jitterPixels + 1));
        var height = Math.Max(MinHeight, pick.Height + rng.Next(-jitterPixels, jitterPixels + 1));
        return new(width, height);
    }
}
