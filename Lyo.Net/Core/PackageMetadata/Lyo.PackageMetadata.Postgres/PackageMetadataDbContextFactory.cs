using Lyo.Exceptions;
using Lyo.PackageMetadata.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.PackageMetadata.Postgres;

/// <summary>Design-time factory for EF Core migrations.</summary>
public sealed class PackageMetadataDbContextFactory : IDesignTimeDbContextFactory<PackageMetadataDbContext>
{
    public PackageMetadataDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PACKAGE_METADATA_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString,
            "PACKAGE_METADATA_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<PackageMetadataDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresPackageMetadataOptions.Schema));
        return new(optionsBuilder.Options);
    }
}
