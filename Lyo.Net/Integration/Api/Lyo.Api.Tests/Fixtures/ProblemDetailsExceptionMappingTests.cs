using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Api.Tests.Fixtures;

/// <summary>Tests for <see cref="LyoProblemDetailsBuilder.FromException" /> mapping of the Lyo.Exceptions HTTP hierarchy to problem details.</summary>
public class ProblemDetailsExceptionMappingTests
{
    [Fact]
    public void FromException_NotFoundException_Maps404WithDefaultCode()
    {
        var err = LyoProblemDetailsBuilder.FromException(new NotFoundException("Widget", 42)).Build();
        Assert.Equal(404, err.Status);
        Assert.Equal(Constants.ApiErrorCodes.NotFound, err.Errors[0].Code);
        Assert.Contains("Widget", err.Detail);
    }

    [Fact]
    public void FromException_HttpException_PreservesExplicitErrorCode()
    {
        var ex = new BadRequestException("Duplicate widget name.") { ErrorCode = "widget.duplicate_name" };
        var err = LyoProblemDetailsBuilder.FromException(ex).Build();
        Assert.Equal(400, err.Status);
        Assert.Equal("widget.duplicate_name", err.Errors[0].Code);
    }

    [Fact]
    public void FromException_ErrorCodeParameter_WinsOverExceptionCode()
    {
        var ex = new BadRequestException("bad") { ErrorCode = "from.exception" };
        var err = LyoProblemDetailsBuilder.FromException(ex, "from.parameter").Build();
        Assert.Equal("from.parameter", err.Errors[0].Code);
    }

    [Fact]
    public void FromException_ValidationException_Maps400WithFieldErrors()
    {
        var ex = new ValidationException(
            new Dictionary<string, IReadOnlyList<string>> {
                ["Name"] = new List<string> { "Name is required.", "Name is too short." }, ["Age"] = new List<string> { "Age must be positive." }
            });

        var err = LyoProblemDetailsBuilder.FromException(ex).Build();
        Assert.Equal(400, err.Status);
        Assert.Equal(3, err.Errors.Count);
        Assert.All(err.Errors, e => Assert.Equal(Constants.ApiErrorCodes.ValidationFailed, e.Code));
        Assert.Contains(err.Errors, e => e.Description.Contains("Name is too short."));
        Assert.Contains(err.Errors, e => e.Description.StartsWith("Age:"));
        Assert.Equal(LyoProblemDetailsBuilder.DefaultValidationDetailSummary, err.Detail);
    }

    [Fact]
    public void FromException_UnprocessableEntityException_Maps422WithFieldErrors()
    {
        var ex = new UnprocessableEntityException(new Dictionary<string, IReadOnlyList<string>> { ["Email"] = new List<string> { "Email is already taken." } });
        var err = LyoProblemDetailsBuilder.FromException(ex).Build();
        Assert.Equal(422, err.Status);
        Assert.Single(err.Errors);
        Assert.Equal(Constants.ApiErrorCodes.UnprocessableEntity, err.Errors[0].Code);
        Assert.Contains("Email is already taken.", err.Errors[0].Description);
    }

    [Fact]
    public void FromException_RateLimitExceededException_Maps429()
    {
        var err = LyoProblemDetailsBuilder.FromException(new RateLimitExceededException(TimeSpan.FromSeconds(30))).Build();
        Assert.Equal(429, err.Status);
        Assert.Equal(Constants.ApiErrorCodes.TooManyRequests, err.Errors[0].Code);
    }

    [Fact]
    public void FromException_GenericException_KeepsUnknownCodeAndInnerChain()
    {
        var ex = new InvalidOperationException("outer", new InvalidOperationException("inner"));
        var err = LyoProblemDetailsBuilder.FromException(ex).Build();
        Assert.Equal(Constants.ApiErrorCodes.Unknown, err.Errors[0].Code);
        Assert.Equal(2, err.Errors.Count);
        Assert.Contains(err.Errors, e => e.Description == "inner");
    }

    [Theory]
    [InlineData(400, Constants.ApiErrorCodes.InvalidRequest)]
    [InlineData(401, Constants.ApiErrorCodes.Unauthorized)]
    [InlineData(403, Constants.ApiErrorCodes.Forbidden)]
    [InlineData(404, Constants.ApiErrorCodes.NotFound)]
    [InlineData(409, Constants.ApiErrorCodes.Conflict)]
    [InlineData(410, Constants.ApiErrorCodes.Gone)]
    [InlineData(422, Constants.ApiErrorCodes.UnprocessableEntity)]
    [InlineData(429, Constants.ApiErrorCodes.TooManyRequests)]
    [InlineData(503, Constants.ApiErrorCodes.ServiceUnavailable)]
    [InlineData(504, Constants.ApiErrorCodes.GatewayTimeout)]
    [InlineData(418, Constants.ApiErrorCodes.Unknown)]
    public void MapHttpStatusToErrorCode_MapsExpectedCodes(int status, string expectedCode) => Assert.Equal(expectedCode, LyoProblemDetails.MapHttpStatusToErrorCode(status));

    [Theory]
    [InlineData(Constants.ApiErrorCodes.Unauthorized, 401)]
    [InlineData(Constants.ApiErrorCodes.Conflict, 409)]
    [InlineData(Constants.ApiErrorCodes.Gone, 410)]
    [InlineData(Constants.ApiErrorCodes.UnprocessableEntity, 422)]
    [InlineData(Constants.ApiErrorCodes.TooManyRequests, 429)]
    [InlineData(Constants.ApiErrorCodes.ServiceUnavailable, 503)]
    [InlineData(Constants.ApiErrorCodes.GatewayTimeout, 504)]
    [InlineData(Constants.ApiErrorCodes.ValidationFailed, 400)]
    public void MapErrorCodeToHttpStatus_MapsNewCodes(string code, int expectedStatus) => Assert.Equal(expectedStatus, LyoProblemDetails.MapErrorCodeToHttpStatus(code));
}