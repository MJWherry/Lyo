using Lyo.Api.Client;
using Lyo.Api.Models.Error;
using Lyo.Exceptions.Models;

namespace Lyo.Api.Tests.Fixtures;

/// <summary>Tests that <see cref="ApiException" /> participates in the Lyo.Exceptions HTTP hierarchy.</summary>
public class ApiExceptionTests
{
    [Fact]
    public void ApiException_DerivesFromHttpException()
    {
        var ex = new ApiException(404, "not found");
        Assert.IsAssignableFrom<HttpException>(ex);
        Assert.Equal(404, ex.StatusCode);
    }

    [Theory]
    [InlineData(408, true)]
    [InlineData(429, true)]
    [InlineData(502, true)]
    [InlineData(503, true)]
    [InlineData(504, true)]
    [InlineData(400, false)]
    [InlineData(404, false)]
    [InlineData(409, false)]
    [InlineData(500, false)]
    public void IsTransient_FollowsStatusCode(int statusCode, bool expected)
        => Assert.Equal(expected, new ApiException(statusCode, "failed").IsTransient);

    [Fact]
    public void ErrorCode_CanBeSetViaInitializer()
    {
        var ex = new ApiException(409, "conflict") { ErrorCode = "widget.duplicate_name" };
        Assert.Equal("widget.duplicate_name", ex.ErrorCode);
    }

    [Fact]
    public void Detail_PrefersProblemDetailsFullMessage()
    {
        var problem = LyoProblemDetails.FromCode("NotFound", "Widget 42 was not found.");
        var ex = new ApiException(404, "generic message", problem);
        Assert.Equal(problem.GetFullMessage(), ex.Detail);
        Assert.Equal("generic message", new ApiException(404, "generic message").Detail);
    }

    [Fact]
    public void CanBeCaughtAsHttpException()
    {
        HttpException? caught;
        try {
            throw new ApiException(503, "unavailable");
        }
        catch (HttpException ex) {
            caught = ex;
        }

        Assert.NotNull(caught);
        Assert.True(caught.IsTransient);
        Assert.Equal(503, caught.StatusCode);
    }
}
