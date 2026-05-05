using System.Linq.Expressions;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common.Request;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Query and get operations: filtered/sorted/paged <see cref="QueryReq" />, projected <see cref="ProjectionQueryReq" />, and single-entity loads with optional includes.</summary>
public interface IQueryService<TContext>
    where TContext : DbContext
{
    /// <summary>Runs a query, maps each row to <typeparamref name="TResult" /> via <see cref="ILyoMapper" />.</summary>
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

    /// <summary>Runs <see cref="ProjectionQueryReq" /> (sparse <c>Select</c>, optional computed fields) and returns projected rows.</summary>
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

    /// <summary>Loads one entity by primary key values, applies optional hooks, maps to <typeparamref name="TResult" />.</summary>
    Task<TResult?> Get<TDbModel, TResult>(
        object[] keys,
        IEnumerable<string>? includes = null,
        Action<GetContext<TDbModel, TContext>>? before = null,
        Action<GetContext<TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;

    /// <summary>Loads one untracked entity by primary key with optional navigation includes.</summary>
    Task<TDbModel?> Get<TDbModel>(object[] keys, IEnumerable<string>? includes = null, CancellationToken ct = default)
        where TDbModel : class;

    /// <summary>Loads and maps by key; <paramref name="includes" /> are enum names (flags expanded to multiple include strings).</summary>
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