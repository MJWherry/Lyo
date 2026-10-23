using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace Lyo.Testing.Containers;

/// <summary>Settings for <see cref="PostgresTestContainer" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PostgresContainerOptions
{
    /// <summary>Docker image for PostgreSQL (default <c>postgres:16-alpine</c>).</summary>
    public string Image { get; set; } = "postgres:16-alpine";

    /// <summary>Optional hook to tweak the Testcontainers builder before <see cref="PostgreSqlBuilder.Build" />.</summary>
    public Action<PostgreSqlBuilder>? ConfigureBuilder { get; set; }

    /// <summary>Docker image. The builder hook is omitted.</summary>
    public override string ToString() => $"Image={Image}";
}