using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.HomeInventory.Postgres.Database;

/// <summary>EF migrations factory for EF Core migrations.</summary>
public class HomeInventoryDbContextFactory : IDesignTimeDbContextFactory<HomeInventoryDbContext>
{
    /// <inheritdoc />
    public HomeInventoryDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<HomeInventoryDbContext>("HOME_INVENTORY_CONNECTION_STRING", "home_inventory"));
}