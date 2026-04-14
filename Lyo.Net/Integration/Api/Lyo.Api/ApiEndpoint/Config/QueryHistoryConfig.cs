using System.Linq.Expressions;

namespace Lyo.Api.ApiEndpoint.Config;

public sealed record QueryHistoryConfig<TDbEntity>
{
    public string GroupName { get; init; } = null!;

    public Expression<Func<TDbEntity, object?>> DefaultOrder { get; set; } = null!;

    public Expression<Func<TDbEntity, DateTime>> StartTimeSelector { get; set; } = null!;

    public Expression<Func<TDbEntity, DateTime>> EndTimeSelector { get; set; } = null!;

    public EndpointAuth? Auth { get; init; }
}