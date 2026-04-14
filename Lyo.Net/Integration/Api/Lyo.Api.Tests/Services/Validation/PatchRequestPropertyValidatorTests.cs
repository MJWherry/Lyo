using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Services.Crud.Validation;

namespace Lyo.Api.Tests.Services.Validation;

public sealed class PatchRequestPropertyValidatorTests
{
    private sealed class SampleEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int RequiredInt { get; set; }

        public int? NullableInt { get; set; }
    }

    [Fact]
    public void Validate_UnknownProperty_ReturnsInvalidField()
    {
        var req = new PatchRequest { Properties = new() { ["NotAProp"] = 1 } };
        var issues = PatchRequestPropertyValidator.Validate<SampleEntity>(req);
        var e = Assert.Single(issues);
        Assert.Equal(Constants.ApiErrorCodes.InvalidField, e.Code);
        Assert.Contains("'NotAProp'", e.Description, StringComparison.Ordinal);
        Assert.Contains(nameof(SampleEntity), e.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_NullToNonNullableValueType_ReturnsInvalidField()
    {
        var req = new PatchRequest { Properties = new() { [nameof(SampleEntity.RequiredInt)] = null } };
        var issues = PatchRequestPropertyValidator.Validate<SampleEntity>(req);
        var e = Assert.Single(issues);
        Assert.Equal(Constants.ApiErrorCodes.InvalidField, e.Code);
        Assert.Contains(nameof(SampleEntity.RequiredInt), e.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_IncompatibleValueType_ReturnsInvalidField()
    {
        var req = new PatchRequest { Properties = new() { [nameof(SampleEntity.Id)] = "not-an-int" } };
        var issues = PatchRequestPropertyValidator.Validate<SampleEntity>(req);
        var e = Assert.Single(issues);
        Assert.Equal(Constants.ApiErrorCodes.InvalidField, e.Code);
        Assert.Contains(nameof(SampleEntity.Id), e.Description, StringComparison.Ordinal);
        Assert.Contains("int", e.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ValidConversions_ReturnsEmpty()
    {
        var req = PatchRequestBuilder.ForId(1)
            .SetProperty(nameof(SampleEntity.Id), 42)
            .SetProperty(nameof(SampleEntity.Name), "x")
            .SetProperty(nameof(SampleEntity.NullableInt), null)
            .Build();

        var issues = PatchRequestPropertyValidator.Validate<SampleEntity>(req);
        Assert.Empty(issues);
    }
}
