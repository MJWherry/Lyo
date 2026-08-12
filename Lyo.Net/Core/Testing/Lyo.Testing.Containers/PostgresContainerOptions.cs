using Testcontainers.PostgreSql;

namespace Lyo.Testing.Containers;

/// <summary>Configuration for <see cref="PostgresTestContainer" />.</summary>
public sealed class PostgresContainerOptions
{
    /// <summary>Docker image for PostgreSQL (default: postgres:16-alpine).</summary>
    public string Image { get; set; } = "postgres:16-alpine";

    /// <summary>Optional customization of the Testcontainers builder before <see cref="PostgreSqlBuilder.Build" />.</summary>
    public Action<PostgreSqlBuilder>? ConfigureBuilder { get; set; }
}