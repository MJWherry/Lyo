using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

public sealed record MetadataConfiguration<TDbContext, TDbEntity>
    where TDbContext : DbContext where TDbEntity : class
{
    public bool IncludeEntityMetadata { get; init; }
}