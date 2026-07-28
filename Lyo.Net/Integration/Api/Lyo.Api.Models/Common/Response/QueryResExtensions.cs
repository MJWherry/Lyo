using Lyo.Exceptions;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Models.Common.Response;

/// <summary>Paging helpers that clone a result's echoed query request with a new <see cref="QueryRequestBase.Start" />.</summary>
public static class QueryResExtensions
{
    /// <summary>Clones the echoed projected/root request and sets <paramref name="start" />.</summary>
    public static QueryRequestBase WithStart<T>(this ProjectedQueryRes<T> result, int start)
    {
        ArgumentHelpers.ThrowIfNull(result);
        var clone = QueryRequestClone.Clone(result.QueryRequest);
        clone.Start = start;
        return clone;
    }

    /// <summary>Clones the echoed projected/root request with <c>Start</c> advanced by one page.</summary>
    public static QueryRequestBase ToNextQueryRequest<T>(this ProjectedQueryRes<T> result)
    {
        ArgumentHelpers.ThrowIfNull(result);
        return result.WithStart(ComputeNextStart(result.Start, result.QueryRequest, result.Amount, result.Items?.Count));
    }

    /// <summary>Same as <see cref="ToNextQueryRequest{T}(ProjectedQueryRes{T})" /> when the echoed request is a <see cref="ProjectionQueryReq" />.</summary>
    public static ProjectionQueryReq ToNextProjectionQueryRequest<T>(this ProjectedQueryRes<T> result)
    {
        ArgumentHelpers.ThrowIfNull(result);
        if (result.QueryRequest is not ProjectionQueryReq)
            throw new InvalidOperationException($"Echoed query request is {result.QueryRequest.GetType().Name}, expected {nameof(ProjectionQueryReq)}.");

        return (ProjectionQueryReq)result.ToNextQueryRequest();
    }

    /// <summary>Same as <see cref="ToNextQueryRequest{T}(ProjectedQueryRes{T})" /> when the echoed request is a <see cref="QueryReq" />.</summary>
    public static QueryReq ToNextRootQueryRequest<T>(this ProjectedQueryRes<T> result)
    {
        ArgumentHelpers.ThrowIfNull(result);
        if (result.QueryRequest is not QueryReq)
            throw new InvalidOperationException($"Echoed query request is {result.QueryRequest.GetType().Name}, expected {nameof(QueryReq)}.");

        return (QueryReq)result.ToNextQueryRequest();
    }

    private static int ComputeNextStart(int? resultStart, QueryRequestBase queryRequest, int? resultAmount, int? itemsCount)
        => (resultStart ?? queryRequest.Start ?? 0) + (queryRequest.Amount ?? resultAmount ?? itemsCount ?? 0);

    extension<T>(QueryRes<T> result)
    {
        /// <summary>Clones the echoed concrete request and sets <paramref name="start" />.</summary>
        public QueryConcreteReq WithStart(int start)
        {
            ArgumentHelpers.ThrowIfNull(result);
            var clone = QueryRequestClone.Clone(result.QueryRequest);
            clone.Start = start;
            return clone;
        }

        /// <summary>Clones the echoed concrete request with <c>Start</c> advanced by one page.</summary>
        public QueryConcreteReq ToNextQueryRequest()
        {
            ArgumentHelpers.ThrowIfNull(result);
            return result.WithStart(ComputeNextStart(result.Start, result.QueryRequest, result.Amount, result.Items?.Count));
        }
    }
}