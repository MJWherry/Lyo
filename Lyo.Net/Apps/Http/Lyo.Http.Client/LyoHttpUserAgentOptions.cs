using System.Diagnostics;
using Lyo.Common.Core.Net;
using Lyo.Exceptions;

namespace Lyo.Http.Client;

/// <summary>Desktop Chrome User-Agent pool and rotation. Vendors leave this off; Flared turns it on with <see cref="LyoHttpUserAgentRotation.PerSession" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LyoHttpUserAgentOptions
{
    /// <summary>Chrome User-Agent for this host. Replace <see cref="Agents" /> to rotate through a custom pool.</summary>
    public static string DefaultChromeAgent => UserAgentGenerator.Generate(UserAgentFormat.Chromium, UserAgentGenerator.ChromeVersion);

    /// <summary>When false, the client does not set User-Agent from this pool (Api.Client stamps <c>Lyo/{version}</c> instead).</summary>
    public bool Enabled { get; set; }

    /// <summary>Pool to rotate through. Defaults to <see cref="DefaultChromeAgent" />.</summary>
    public string[] Agents { get; set; } = [DefaultChromeAgent];

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
            return DefaultChromeAgent;

        var index = Math.Abs(seed) % agents.Length;
        return agents[index];
    }

    /// <summary>Whether the pool is on, how it rotates, and how many agents it holds.</summary>
    public override string ToString() => $"Enabled={Enabled} Rotation={Rotation} Agents={Agents?.Length ?? 0}";
}