using System.Linq.Expressions;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common.Request;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Read.Query;

public interface IQueryService<TContext>
    where TContext : DbContext
{
    Task<QueryRes<TResult>> Query<TDbModel, TResult>(
        QueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class;

    /// <summary>Returns raw entities without mapping. Entities are always untracked by the context.</summary>
    Task<QueryRes<TDbModel>> Query<TDbModel>(
        QueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<ProjectedQueryRes<object?>> QueryProjected<TDbModel>(
        ProjectionQueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class;

    //Task<QueryResults<TResult>> QueryWithTree<TDbModel, TResult>(
    //    WhereClause queryTree,
    //    int? start = null,
    //    int? amount = null,
    //    IEnumerable<string>? includes = null,
    //    SortBy[]? sortBy = null,
    //    Expression<Func<TDbModel, object?>>? defaultOrder = null,
    //    CancellationToken ct = default)
    //    where TDbModel : class;

    Task<TResult?> Get<TDbModel, TResult>(
        object[] keys,
        IEnumerable<string>? includes = null,
        Action<GetContext<TDbModel, TContext>>? before = null,
        Action<GetContext<TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<TDbModel?> Get<TDbModel>(object[] keys, IEnumerable<string>? includes = null, CancellationToken ct = default)
        where TDbModel : class;

    Task<TResult?> Get<TDbModel, TResult>(object[] keys, CancellationToken ct = default, params Enum[]? includes)
        where TDbModel : class
    {
        var includeStrings = includes?.SelectMany(enumValue => {
                var enumType = enumValue.GetType();
                if (enumType.IsDefined(typeof(FlagsAttribute), false))
                    return Enum.GetValues(enumType).Cast<Enum>().Where(enumValue.HasFlag).Where(flag => Convert.ToInt32(flag) != 0).Select(flag => flag.ToString());

                return [enumValue.ToString()];
            })
            .ToList();

        return Get<TDbModel, TResult>(keys, includeStrings, null, null, ct);
    }
}