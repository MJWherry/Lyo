using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Diagram.Postgres.Database;

/// <summary>Design-time factory for EF migrations. Needs <c>DIAGRAM_CONNECTION_STRING</c>.</summary>
public sealed class DiagramDbContextFactory : IDesignTimeDbContextFactory<DiagramDbContext>
{
    /// <inheritdoc />
    public DiagramDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<DiagramDbContext>("DIAGRAM_CONNECTION_STRING", PostgresDiagramOptions.Schema));
}
