using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Audit.Postgres.Database;

/// <summary>Design-time factory that builds <see cref="AuditDbContext" /> for EF migrations.</summary>
public class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    /// <inheritdoc />
    public AuditDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<AuditDbContext>("AUDIT_CONNECTION_STRING", "audit"));
}