using System.Linq.Expressions;

namespace Lyo.Api.ApiEndpoint.Config;

public sealed record ExportConfig<TDbEntity>
{
    public string GroupName { get; init; } = null!;

    public Expression<Func<TDbEntity, object?>> DefaultOrder { get; init; } = null!;

    public EndpointAuth? Auth { get; init; }

    /// <summary>
    /// Property names (or dotted paths) that exports may never select or reference in column templates.
    /// A bare name also denies any nested path ending in that name (e.g. <c>EncryptedValue</c> denies <c>Parameters.EncryptedValue</c>).
    /// Use for sensitive entity columns that response mapping would otherwise mask.
    /// </summary>
    public IReadOnlyCollection<string>? DeniedSelectFields { get; init; }
}