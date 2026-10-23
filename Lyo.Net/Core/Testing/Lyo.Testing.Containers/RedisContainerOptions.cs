using System.Diagnostics;
using Testcontainers.Redis;

namespace Lyo.Testing.Containers;

/// <summary>Settings for <see cref="RedisTestContainer" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RedisContainerOptions
{
    /// <summary>Docker image for Redis (default <c>redis:7-alpine</c>).</summary>
    public string Image { get; set; } = "redis:7-alpine";

    /// <summary>Optional hook to tweak the Testcontainers builder before <see cref="RedisBuilder.Build" />.</summary>
    public Action<RedisBuilder>? ConfigureBuilder { get; set; }

    /// <summary>Docker image. The builder hook is omitted.</summary>
    public override string ToString() => $"Image={Image}";
}