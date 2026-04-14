using System.Linq.Expressions;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Enums;

namespace Lyo.Api.Services.Crud.Read.Query;

public interface IQueryHistoryService<in TContext>
{
    Task<QueryHistoryResults<HistoryResult<TResult>>> QueryHistory<TDbModel, TResult>(
        HistoryQuery query,
        Expression<Func<TDbModel, object?>> defaultOrder,
        Func<TDbModel, DateTime> startTimeSelector,
        Func<TDbModel, DateTime> endTimeSelector,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class;
}