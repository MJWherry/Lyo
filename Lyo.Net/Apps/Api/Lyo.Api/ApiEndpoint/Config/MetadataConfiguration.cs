using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record MetadataConfiguration<TDbContext, TDbEntity>
    where TDbContext : DbContext where TDbEntity : class
{
    public bool IncludeEntityMetadata { get; init; }
}