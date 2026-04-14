using Lyo.Api.Models;
using Lyo.Api.Services.Crud.Validation;

namespace Lyo.Api.Tests.Services.Validation;

public sealed class BulkListRequestValidatorTests
{
    [Fact]
    public void Validate_AtMaxAllowed_Succeeds()
    {
        var r = BulkListRequestValidator.Validate(new BulkListRequestValidatorInput(100, 100));

        Assert.True(r.IsSuccess);
        Assert.NotNull(r.Data);
        Assert.Equal(100, r.Data!.Count);
    }

    [Fact]
    public void Validate_BelowMaxAllowed_Succeeds()
    {
        var r = BulkListRequestValidator.Validate(new BulkListRequestValidatorInput(1, 500));

        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void Validate_AboveMaxAllowed_FailsWithExceedMaxBulkSize()
    {
        var r = BulkListRequestValidator.Validate(new BulkListRequestValidatorInput(11, 10));

        Assert.False(r.IsSuccess);
        var err = Assert.Single(r.Errors!);
        Assert.Equal(Constants.ApiErrorCodes.ExceedMaxBulkSize, err.Code);
        Assert.Contains("10", err.Message, StringComparison.Ordinal);
        Assert.Contains("11", err.Message, StringComparison.Ordinal);
    }
}
