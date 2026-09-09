using Lyo.PackageMetadata.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.PackageMetadata.Postgres;

/// <summary>Design-time factory that mints a context for EF Core migrations.</summary>
public sealed class PackageMetadataDbContextFactory : IDesignTimeDbContextFactory<PackageMetadataDbContext>
{
    /// <inheritdoc />
    public PackageMetadataDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<PackageMetadataDbContext>("PACKAGE_METADATA_CONNECTION_STRING", PostgresPackageMetadataOptions.Schema));
}