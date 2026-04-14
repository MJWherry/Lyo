using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read;
using Lyo.Query.Models.Common.Request;
using Lyo.Validation;

namespace Lyo.Api.Services.Crud.Validation;

/// <summary>Validates <c>Start</c> / <c>Amount</c> against <see cref="QueryOptions" /> min/max bounds.</summary>
public static class QueryPagingBoundsValidator
{
    /// <summary>Standard list/query/projected query: <paramref name="maxAmount" /> is typically <see cref="QueryOptions.MaxPageSize" />.</summary>
    public static IReadOnlyList<ApiError> Validate(QueryRequestBase request, QueryOptions options, int maxAmount)
        => ValidatePaging(request.Start, request.Amount, options, maxAmount);

    /// <summary>Temporal/history query: same bounds as list/query paging.</summary>
    public static IReadOnlyList<ApiError> Validate(HistoryQuery query, QueryOptions options)
        => ValidatePaging(query.Start, query.Amount, options, options.MaxPageSize);

    private sealed record SingleInt(int Value);

    private static List<ApiError> ValidatePaging(int? start, int? amount, QueryOptions options, int maxAmount)
    {
        var errors = new List<ApiError>();
        if (start.HasValue) {
            var r = ValidatorBuilder<SingleInt>.Create()
                .RuleFor(x => x.Value)
                .InclusiveBetween(
                    options.MinPagingStart,
                    options.MaxPagingStart,
                    Constants.ApiErrorCodes.InvalidPaging,
                    $"Start must be between {options.MinPagingStart} and {options.MaxPagingStart} inclusive (received {start.Value}).")
                .Build()
                .Validate(new SingleInt(start.Value));

            if (!r.IsSuccess && r.Errors is { Count: > 0 } e)
                errors.Add(new ApiError(e[0].Code, e[0].Message));
        }

        if (amount.HasValue) {
            var r = ValidatorBuilder<SingleInt>.Create()
                .RuleFor(x => x.Value)
                .InclusiveBetween(
                    options.MinPagingAmount,
                    maxAmount,
                    Constants.ApiErrorCodes.InvalidPaging,
                    $"Amount must be between {options.MinPagingAmount} and {maxAmount} inclusive (received {amount.Value}).")
                .Build()
                .Validate(new SingleInt(amount.Value));

            if (!r.IsSuccess && r.Errors is { Count: > 0 } e)
                errors.Add(new(e[0].Code, e[0].Message));
        }

        return errors;
    }
}
