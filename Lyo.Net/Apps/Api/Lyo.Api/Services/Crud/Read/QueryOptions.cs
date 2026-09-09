using System.Diagnostics;

namespace Lyo.Api.Services.Crud.Read;

/// <summary>Singleton settings bound from configuration: paging limits, export cap, split queries, wildcard projection, and typed cache payloads for query results.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class QueryOptions
{
    /// <summary>Default <c>Amount</c> when the client omits page size, subject to <see cref="MaxPageSize" />.</summary>
    public int DefaultPageSize { get; init; } = 100;

    /// <summary>Largest allowed page size for query and projection requests.</summary>
    public int MaxPageSize { get; init; } = 2000;

    /// <summary>Smallest <c>Start</c> offset for query, history, and export bodies (inclusive).</summary>
    public int MinPagingStart { get; init; } = 0;

    /// <summary>Largest <c>Start</c> offset for query, history, and export bodies (inclusive).</summary>
    public int MaxPagingStart { get; init; } = 10_000_000;

    /// <summary>When <c>Amount</c> is set, it must be at least this value (usually 1).</summary>
    public int MinPagingAmount { get; init; } = 1;

    /// <summary>Most rows allowed for export operations. Exports are capped to this value.</summary>
    public int MaxExportSize { get; init; } = 5000;

    /// <summary>When <c>true</c>, EF may split include graphs into more than one SQL command.</summary>
    public bool EnableSplitQueries { get; init; } = true;

    /// <summary>When <c>true</c>, include-heavy query paging prefers an ID-first strategy to avoid paging on fan-out join shapes.</summary>
    public bool EnableIdFirstIncludePaging { get; init; } = true;

    /// <summary>When <c>true</c>, read queries use no-tracking with identity resolution so graphs stay consistent.</summary>
    public bool UseNoTrackingWithIdentityResolution { get; init; } = true;

    /// <summary>
    /// When <c>true</c> (default), <c>QueryProject</c> <c>Select</c> may use terminal <c>*</c> (e.g. collection scope wildcards). Set via API host configuration (singleton
    /// <see cref="QueryOptions" />). When <c>false</c>, paths containing <c>*</c> are rejected.
    /// </summary>
    public bool AllowSelectWildcards { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, query results use typed <c>ICacheService.GetOrSetPayloadAsync&lt;T&gt;</c> (via <see cref="Lyo.Cache.ICachePayloadSerializer" /> and
    /// <see cref="Lyo.Cache.ICachePayloadCodec" />; optional compress/encrypt per <see cref="Lyo.Cache.CacheOptions.Payload" />) instead of Fusion CLR serialization. Requires cache
    /// registration (e.g. <c>AddLocalCache</c> / <c>AddFusionCache</c>).
    /// </summary>
    public bool CacheQueryResultsAsUtf8Payload { get; init; }

    /// <summary>Most include paths allowed in query request bodies.</summary>
    public int MaxIncludePathCount { get; init; } = 16;

    /// <summary>Largest page size allowed when includes are requested. Set to <c>0</c> to disable include-specific page capping.</summary>
    public int MaxIncludePageSize { get; init; } = 300;

    /// <summary>Applies <see cref="MaxIncludePageSize" /> when the include count is at least this value.</summary>
    public int IncludePageSizeCapMinIncludeCount { get; init; } = 1;

    /// <summary>Most key sets allowed in query request bodies.</summary>
    public int MaxKeySetCount { get; init; } = 300;

    /// <summary>Most select fields allowed in QueryProject requests.</summary>
    public int MaxSelectFieldCount { get; init; } = 256;

    /// <summary>Most computed fields allowed in QueryProject requests.</summary>
    public int MaxComputedFieldCount { get; init; } = 32;

    /// <summary>Longest computed-template text length per computed field in QueryProject requests.</summary>
    public int MaxComputedTemplateLength { get; init; } = 2048;

    /// <summary>
    /// Row ceiling for the two-phase sub-clause fallback, which loads everything matching the outer filter and evaluates the nested clause in memory. A filter that is broad at
    /// the root would otherwise pull the whole table into the process. Exceeding the ceiling fails the query rather than degrading the host.
    /// </summary>
    public int MaxSubQueryMaterializedRows { get; init; } = 50_000;

    public override string ToString()
        => $"DefaultPageSize={DefaultPageSize} MaxPageSize={MaxPageSize} PagingStart=[{MinPagingStart},{MaxPagingStart}] MinAmount={MinPagingAmount} MaxExportSize={MaxExportSize} SplitQueries={EnableSplitQueries} IdFirstIncludePaging={EnableIdFirstIncludePaging} AllowSelectWildcards={AllowSelectWildcards} CacheQueryUtf8Payload={CacheQueryResultsAsUtf8Payload} MaxIncludes={MaxIncludePathCount} MaxIncludePageSize={MaxIncludePageSize} IncludePageSizeCapMinIncludeCount={IncludePageSizeCapMinIncludeCount} MaxKeySets={MaxKeySetCount} MaxSelectFields={MaxSelectFieldCount} MaxComputedFields={MaxComputedFieldCount} MaxComputedTemplateLength={MaxComputedTemplateLength} MaxSubQueryMaterializedRows={MaxSubQueryMaterializedRows}";
}