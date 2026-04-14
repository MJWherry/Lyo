using System.Linq.Expressions;

namespace Lyo.Api.ApiEndpoint.Config;

public sealed record QueryConfig<TDbEntity>
{
    public string GroupName { get; init; } = null!;

    public Expression<Func<TDbEntity, object?>> DefaultOrder { get; init; } = null!;

    public EndpointAuth? Auth { get; init; }

    public bool EnableComputedFields { get; init; }
}