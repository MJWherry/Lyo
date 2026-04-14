using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Tests.Services.Validation;

public sealed class QueryPagingBoundsValidatorTests
{
    private static readonly QueryOptions DefaultOptions = new();

    [Fact]
    public void Validate_QueryRequest_StartBelowMin_ReturnsInvalidPaging()
    {
        var req = new QueryReq { Start = DefaultOptions.MinPagingStart - 1, Amount = 10 };
        var errors = QueryPagingBoundsValidator.Validate(req, DefaultOptions, DefaultOptions.MaxPageSize);

        var e = Assert.Single(errors);
        Assert.Equal(Constants.ApiErrorCodes.InvalidPaging, e.Code);
        Assert.Contains("Start", e.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_QueryRequest_StartAboveMax_ReturnsInvalidPaging()
    {
        var req = new QueryReq { Start = DefaultOptions.MaxPagingStart + 1 };
        var errors = QueryPagingBoundsValidator.Validate(req, DefaultOptions, DefaultOptions.MaxPageSize);

        var e = Assert.Single(errors);
        Assert.Equal(Constants.ApiErrorCodes.InvalidPaging, e.Code);
    }

    [Fact]
    public void Validate_QueryRequest_AmountBelowMin_ReturnsInvalidPaging()
    {
        var req = new QueryReq { Amount = DefaultOptions.MinPagingAmount - 1 };
        var errors = QueryPagingBoundsValidator.Validate(req, DefaultOptions, DefaultOptions.MaxPageSize);

        var e = Assert.Single(errors);
        Assert.Equal(Constants.ApiErrorCodes.InvalidPaging, e.Code);
        Assert.Contains("Amount", e.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_QueryRequest_AmountAboveMaxPageSize_ReturnsInvalidPaging()
    {
        var req = new QueryReq { Amount = DefaultOptions.MaxPageSize + 1 };
        var errors = QueryPagingBoundsValidator.Validate(req, DefaultOptions, DefaultOptions.MaxPageSize);

        var e = Assert.Single(errors);
        Assert.Equal(Constants.ApiErrorCodes.InvalidPaging, e.Code);
    }

    [Fact]
    public void Validate_QueryRequest_CustomMaxAmount_UsesThatCap()
    {
        const int cap = 50;
        var req = new QueryReq { Amount = cap + 1 };
        var errors = QueryPagingBoundsValidator.Validate(req, DefaultOptions, cap);

        Assert.Single(errors);
        Assert.Contains($"{cap}", errors[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_QueryRequest_ValidPaging_ReturnsEmpty()
    {
        var req = new QueryReq { Start = DefaultOptions.MinPagingStart, Amount = DefaultOptions.MinPagingAmount };
        var errors = QueryPagingBoundsValidator.Validate(req, DefaultOptions, DefaultOptions.MaxPageSize);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_HistoryQuery_InvalidAmount_ReturnsInvalidPaging()
    {
        var q = new HistoryQuery { Amount = 0 };
        var errors = QueryPagingBoundsValidator.Validate(q, DefaultOptions);

        Assert.Single(errors);
        Assert.Equal(Constants.ApiErrorCodes.InvalidPaging, errors[0].Code);
    }
}
