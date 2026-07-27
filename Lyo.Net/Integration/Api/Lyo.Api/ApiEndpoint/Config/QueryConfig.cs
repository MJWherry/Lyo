using System.Linq.Expressions;

namespace Lyo.Api.ApiEndpoint.Config;

public sealed record QueryConfig<TDbEntity>
{
    public string GroupName { get; init; } = null!;

    public Expression<Func<TDbEntity, object?>> DefaultOrder { get; init; } = null!;

    public EndpointAuth? Auth { get; init; }

    public bool EnableComputedFields { get; init; }

    public int? MaxIncludePathCount { get; init; }

    public int? MaxIncludePageSize { get; init; }

    public int? MaxKeySetCount { get; init; }

    public int? MaxSelectFieldCount { get; init; }

    public int? MaxComputedFieldCount { get; init; }

    public int? MaxComputedTemplateLength { get; init; }

    /// <summary>
    /// Property names (or dotted paths) that projected queries may never select or reference in computed templates.
    /// A bare name also denies any nested path ending in that name (e.g. <c>EncryptedValue</c> denies <c>Parameters.EncryptedValue</c>).
    /// Use for sensitive entity columns that response mapping would otherwise mask.
    /// </summary>
    public IReadOnlyCollection<string>? DeniedSelectFields { get; init; }
}