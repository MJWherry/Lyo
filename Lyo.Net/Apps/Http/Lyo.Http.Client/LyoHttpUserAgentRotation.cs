namespace Lyo.Http.Client;

/// <summary>How often <see cref="LyoHttpUserAgentOptions" /> picks a User-Agent from the pool.</summary>
public enum LyoHttpUserAgentRotation
{
    /// <summary>Always use the first agent (or <see cref="LyoHttpUserAgentOptions.DefaultAgent" />).</summary>
    None = 0,

    /// <summary>One agent for the lifetime of this client instance.</summary>
    PerClient = 1,

    /// <summary>One agent when a logical session starts; pin it until the next session.</summary>
    PerSession = 2,

    /// <summary>New agent on every send. Never use with Flared clearance cookies.</summary>
    PerRequest = 3
}
