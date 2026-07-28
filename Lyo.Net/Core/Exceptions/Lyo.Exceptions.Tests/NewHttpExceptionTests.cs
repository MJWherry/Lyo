using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class BadRequestExceptionTests
{
    [Fact]
    public void Default_Has400AndDefaultMessage()
    {
        var ex = new BadRequestException();
        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("The request is invalid.", ex.Message);
        Assert.Null(ex.ParameterName);
    }

    [Fact]
    public void MessageConstructor_UsesMessage()
    {
        var ex = new BadRequestException("Body is malformed.");
        Assert.Equal("Body is malformed.", ex.Message);
    }

    [Fact]
    public void MessageConstructor_WithInner_SetsInnerException()
    {
        var inner = new FormatException("cause");
        var ex = new BadRequestException("Body is malformed.", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ParameterConstructor_SetsParameterName()
    {
        var ex = new BadRequestException("Page size must be positive.", "pageSize");
        Assert.Equal("pageSize", ex.ParameterName);
        Assert.Equal("Page size must be positive.", ex.Message);
    }

    [Fact]
    public void IsTransient_IsFalse() => Assert.False(new BadRequestException().IsTransient);
}

public class UnprocessableEntityExceptionTests
{
    [Fact]
    public void Default_Has422AndEmptyErrors()
    {
        var ex = new UnprocessableEntityException();
        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("The request could not be processed.", ex.Message);
        Assert.Empty(ex.Errors);
    }

    [Fact]
    public void MessageConstructor_UsesMessage()
    {
        var ex = new UnprocessableEntityException("Cannot process.");
        Assert.Equal("Cannot process.", ex.Message);
        Assert.Empty(ex.Errors);
    }

    [Fact]
    public void MessageConstructor_WithInner_SetsInnerException()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new UnprocessableEntityException("Cannot process.", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ErrorsConstructor_PopulatesErrorsAndMessageDetails()
    {
        var errors = new Dictionary<string, IReadOnlyList<string>> { ["Email"] = new[] { "Email is invalid." } };
        var ex = new UnprocessableEntityException(errors);
        Assert.Single(ex.Errors);
        Assert.Equal("Email is invalid.", ex.Errors["Email"][0]);
        Assert.Contains("Email: Email is invalid.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorsConstructor_WithCustomMessage_StartsWithMessage()
    {
        var errors = new Dictionary<string, IReadOnlyList<string>> { ["Age"] = new[] { "Age must be positive." } };
        var ex = new UnprocessableEntityException(errors, "Entity invalid.");
        Assert.StartsWith("Entity invalid.", ex.Message, StringComparison.Ordinal);
    }
}

public class GoneExceptionTests
{
    [Fact]
    public void Default_Has410AndDefaultMessage()
    {
        var ex = new GoneException();
        Assert.Equal(410, ex.StatusCode);
        Assert.Equal("The requested resource is no longer available.", ex.Message);
        Assert.Null(ex.ResourceName);
    }

    [Fact]
    public void MessageConstructor_WithInner_SetsInnerException()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new GoneException("Removed.", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ForResource_WithId_SetsPropertiesAndMessage()
    {
        var ex = GoneException.ForResource("Report", 7);
        Assert.Equal("Report", ex.ResourceName);
        Assert.Equal(7, ex.ResourceId);
        Assert.Equal("Report with ID '7' is no longer available.", ex.Message);
    }

    [Fact]
    public void ForResource_WithoutId_SetsNameOnly()
    {
        var ex = GoneException.ForResource("Report");
        Assert.Equal("Report", ex.ResourceName);
        Assert.Null(ex.ResourceId);
        Assert.Equal("Report is no longer available.", ex.Message);
    }
}

public class GatewayTimeoutExceptionTests
{
    [Fact]
    public void Default_Has504AndDefaultMessage()
    {
        var ex = new GatewayTimeoutException();
        Assert.Equal(504, ex.StatusCode);
        Assert.Equal("The upstream service did not respond in time.", ex.Message);
    }

    [Fact]
    public void IsTransient_IsTrue() => Assert.True(new GatewayTimeoutException().IsTransient);

    [Fact]
    public void ServiceConstructor_SetsServiceNameAndTimeout()
    {
        var ex = new GatewayTimeoutException("billing-api", TimeSpan.FromSeconds(30));
        Assert.Equal("billing-api", ex.ServiceName);
        Assert.Equal(TimeSpan.FromSeconds(30), ex.Timeout);
        Assert.Contains("billing-api", ex.Message, StringComparison.Ordinal);
        Assert.Contains("30", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceConstructor_WithoutTimeout_OmitsTimeoutFromMessage()
    {
        var ex = new GatewayTimeoutException("billing-api", (TimeSpan?)null);
        Assert.Null(ex.Timeout);
        Assert.DoesNotContain("Timeout:", ex.Message, StringComparison.Ordinal);
    }
}

public class HttpExceptionsFactoryTests
{
    [Theory]
    [InlineData(400, typeof(BadRequestException))]
    [InlineData(401, typeof(UnauthorizedException))]
    [InlineData(403, typeof(ForbiddenException))]
    [InlineData(404, typeof(NotFoundException))]
    [InlineData(409, typeof(ConflictException))]
    [InlineData(410, typeof(GoneException))]
    [InlineData(422, typeof(UnprocessableEntityException))]
    [InlineData(429, typeof(RateLimitExceededException))]
    [InlineData(503, typeof(ServiceUnavailableException))]
    [InlineData(504, typeof(GatewayTimeoutException))]
    public void FromStatusCode_KnownCode_ReturnsDedicatedType(int statusCode, Type expectedType)
    {
        var ex = HttpExceptions.FromStatusCode(statusCode, "boom");
        Assert.IsType(expectedType, ex);
        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal("boom", ex.Message);
    }

    [Theory]
    [InlineData(402)]
    [InlineData(418)]
    [InlineData(500)]
    [InlineData(502)]
    public void FromStatusCode_UnknownCode_ReturnsGenericHttpException(int statusCode)
    {
        var ex = HttpExceptions.FromStatusCode(statusCode, "boom");
        var generic = Assert.IsType<GenericHttpException>(ex);
        Assert.Equal(statusCode, generic.StatusCode);
    }

    [Fact]
    public void FromStatusCode_PropagatesInnerException()
    {
        var inner = new TimeoutException("cause");
        var ex = HttpExceptions.FromStatusCode(504, "boom", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Theory]
    [InlineData(408, true)]
    [InlineData(502, true)]
    [InlineData(418, false)]
    [InlineData(500, false)]
    public void GenericHttpException_IsTransient_FollowsStatusCode(int statusCode, bool expected)
        => Assert.Equal(expected, new GenericHttpException(statusCode, "boom").IsTransient);
}

public class HttpExceptionErrorCodeAndTransientTests
{
    [Fact]
    public void ErrorCode_DefaultsToNull() => Assert.Null(new NotFoundException().ErrorCode);

    [Fact]
    public void ErrorCode_SettableViaObjectInitializer()
    {
        var ex = new NotFoundException("User not found.") { ErrorCode = "user.not_found" };
        Assert.Equal("user.not_found", ex.ErrorCode);
    }

    [Fact]
    public void IsTransient_DefaultsToFalse()
    {
        Assert.False(new NotFoundException().IsTransient);
        Assert.False(new ConflictException().IsTransient);
        Assert.False(new ForbiddenException().IsTransient);
    }

    [Fact]
    public void IsTransient_TrueForRetryableTypes()
    {
        Assert.True(new RateLimitExceededException().IsTransient);
        Assert.True(new ServiceUnavailableException().IsTransient);
        Assert.True(new GatewayTimeoutException().IsTransient);
    }

    [Fact]
    public void IsTransient_UsableAsBasePredicate()
    {
        HttpException ex = new RateLimitExceededException();
        Assert.True(ex.IsTransient);
    }
}

public class ForResourceFactoryTests
{
    [Fact]
    public void NotFound_ForResource_WithNullId_SetsResourceName()
    {
        // Regression: new NotFoundException("User", null) binds to (message, Exception?) and never sets ResourceName.
        var ex = NotFoundException.ForResource("User");
        Assert.Equal("User", ex.ResourceName);
        Assert.Null(ex.ResourceId);
        Assert.Equal("User was not found.", ex.Message);
    }

    [Fact]
    public void NotFound_ForResource_WithId_SetsProperties()
    {
        var ex = NotFoundException.ForResource("User", 42);
        Assert.Equal("User", ex.ResourceName);
        Assert.Equal(42, ex.ResourceId);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public void NotFound_ForResource_WithInner_SetsInnerException()
    {
        var inner = new InvalidOperationException("cause");
        var ex = NotFoundException.ForResource("User", 42, inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void Conflict_ForResource_WithNullId_SetsResourceName()
    {
        var ex = ConflictException.ForResource("Order");
        Assert.Equal("Order", ex.ResourceName);
        Assert.Null(ex.ResourceId);
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public void Conflict_ForResource_WithId_SetsProperties()
    {
        var ex = ConflictException.ForResource("Order", "abc");
        Assert.Equal("Order", ex.ResourceName);
        Assert.Equal("abc", ex.ResourceId);
    }

    [Fact]
    public void Forbidden_ForResource_SetsAllProperties()
    {
        var ex = ForbiddenException.ForResource("Report", 7, "Missing admin role");
        Assert.Equal("Report", ex.ResourceName);
        Assert.Equal(7, ex.ResourceId);
        Assert.Equal("Missing admin role", ex.Reason);
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void Forbidden_ForResource_WithNullId_SetsResourceName()
    {
        var ex = ForbiddenException.ForResource("Report");
        Assert.Equal("Report", ex.ResourceName);
        Assert.Null(ex.ResourceId);
    }
}