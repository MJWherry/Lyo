using System.Diagnostics;

namespace Lyo.Api.Services.Crud.Read;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class QueryOptions
{
    public int DefaultPageSize { get; init; } = 100;

    public int MaxPageSize { get; init; } = 2000;

    /// <summary>Minimum <c>Start</c> offset for query/history/export bodies (inclusive).</summary>
    public int MinPagingStart { get; init; } = 0;

    /// <summary>Maximum <c>Start</c> offset for query/history/export bodies (inclusive).</summary>
    public int MaxPagingStart { get; init; } = 10_000_000;

    /// <summary>When <c>Amount</c> is set, it must be at least this value (typically 1).</summary>
    public int MinPagingAmount { get; init; } = 1;

    /// <summary>Maximum number of rows for export operations. Exports are capped to this value.</summary>
    public int MaxExportSize { get; init; } = 5000;

    public bool EnableSplitQueries { get; init; } = true;

    public bool UseNoTrackingWithIdentityResolution { get; init; } = true;

    /// <summary>
    /// When <c>true</c> (default), <c>QueryProject</c> <c>Select</c> may use terminal <c>*</c> (e.g. collection scope wildcards). Set via API host configuration (singleton <see cref="QueryOptions" />).
    /// When <c>false</c>, paths containing <c>*</c> are rejected.
    /// </summary>
    public bool AllowSelectWildcards { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, query results use typed <c>ICacheService.GetOrSetPayloadAsync&lt;T&gt;</c> (via <see cref="Lyo.Cache.ICachePayloadSerializer" /> and <see cref="Lyo.Cache.ICachePayloadCodec" />;
    /// optional compress/encrypt per <see cref="Lyo.Cache.CacheOptions.Payload" />) instead of Fusion CLR serialization. Requires cache registration (e.g. <c>AddLocalCache</c> / <c>AddFusionCache</c>).
    /// </summary>
    public bool CacheQueryResultsAsUtf8Payload { get; init; }

    public override string ToString()
        => $"DefaultPageSize={DefaultPageSize} MaxPageSize={MaxPageSize} PagingStart=[{MinPagingStart},{MaxPagingStart}] MinAmount={MinPagingAmount} MaxExportSize={MaxExportSize} SplitQueries={EnableSplitQueries} AllowSelectWildcards={AllowSelectWildcards} CacheQueryUtf8Payload={CacheQueryResultsAsUtf8Payload}";
}