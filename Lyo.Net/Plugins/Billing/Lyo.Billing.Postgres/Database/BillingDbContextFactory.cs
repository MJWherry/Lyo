using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Billing.Postgres.Database;

/// <summary>EF migrations factory.</summary>
public class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    /// <inheritdoc />
    public BillingDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<BillingDbContext>("BILLING_CONNECTION_STRING", PostgresBillingOptions.Schema));
}
