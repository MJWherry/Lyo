using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.HomeInventory.Postgres.Database;

/// <summary>Design-time factory for EF Core migrations.</summary>
public class HomeInventoryDbContextFactory : IDesignTimeDbContextFactory<HomeInventoryDbContext>
{
    public HomeInventoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HOME_INVENTORY_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "HOME_INVENTORY_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<HomeInventoryDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "home_inventory"));
        return new(optionsBuilder.Options);
    }
}