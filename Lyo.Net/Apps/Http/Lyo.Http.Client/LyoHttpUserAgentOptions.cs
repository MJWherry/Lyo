using Lyo.Exceptions;

namespace Lyo.Http.Client;

/// <summary>Desktop Chrome User-Agent pool and rotation. Vendors leave this off; Flared turns it on with <see cref="LyoHttpUserAgentRotation.PerSession" />.</summary>
public sealed class LyoHttpUserAgentOptions
{
    /// <summary>Current desktop Chrome strings (Windows, macOS, Linux). Replace when Chrome's major version moves.</summary>
    public static readonly string[] DefaultDesktopChromeAgents = [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.7339.80 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.7339.80 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.7339.80 Safari/537.36"
    ];

    /// <summary>When false, the client does not set User-Agent from this pool (Api.Client stamps <c>Lyo/{version}</c> instead).</summary>
    public bool Enabled { get; set; }

    /// <summary>Pool to rotate through. Defaults to <see cref="DefaultDesktopChromeAgents" />.</summary>
    public string[] Agents { get; set; } = DefaultDesktopChromeAgents.ToArray();

    /// <summary>Used when <see cref="Rotation" /> is <see cref="LyoHttpUserAgentRotation.None" /> and this is set.</summary>
    public string? DefaultAgent { get; set; }

    /// <summary>How often to pick a new agent. Default <see cref="LyoHttpUserAgentRotation.None" />.</summary>
    public LyoHttpUserAgentRotation Rotation { get; set; } = LyoHttpUserAgentRotation.None;

    /// <summary>Throws when the pool is empty while enabled.</summary>
    public void Validate()
    {
        if (!Enabled)
            return;

        if (Agents == null || Agents.Length == 0 || Agents.All(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("LyoHttpUserAgentOptions.Agents must contain at least one User-Agent when Enabled is true.");
    }

    /// <summary>Picks an agent for <paramref name="seed" /> (session id, client id, or a random value).</summary>
    public string Resolve(int seed)
    {
        ArgumentHelpers.ThrowIfNull(Agents);
        if (!string.IsNullOrWhiteSpace(DefaultAgent) && Rotation == LyoHttpUserAgentRotation.None)
            return DefaultAgent!;

        var agents = Agents.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
        if (agents.Length == 0)
            return DefaultDesktopChromeAgents[0];

        var index = Math.Abs(seed) % agents.Length;
        return agents[index];
    }
}
