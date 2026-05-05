using System.Linq.Expressions;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Enums;

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Temporal-style history query over entities that expose start/end timestamps (used by QueryHistory endpoints).</summary>
public interface IQueryHistoryService
{
    /// <summary>Filters and projects history rows using <paramref name="startTimeSelector" /> and <paramref name="endTimeSelector" />.</summary>
    Task<QueryHistoryResults<HistoryResult<TResult>>> QueryHistory<TDbModel, TResult>(
        HistoryQuery query,
        Expression<Func<TDbModel, object?>> defaultOrder,
        Func<TDbModel, DateTime> startTimeSelector,
        Func<TDbModel, DateTime> endTimeSelector,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class;
}