using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class NotFoundExceptionTests
{
    [Fact]
    public void Default_Has404AndDefaultMessage()
    {
        var ex = new NotFoundException();
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("The requested resource was not found.", ex.Message);
        Assert.Null(ex.ResourceName);
    }

    [Fact]
    public void MessageConstructor_UsesMessage()
    {
        var ex = new NotFoundException("Custom not found.");
        Assert.Equal("Custom not found.", ex.Message);
        Assert.Null(ex.ResourceName);
    }

    [Fact]
    public void MessageConstructor_WithInner_SetsInnerException()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new NotFoundException("Custom not found.", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ResourceConstructor_WithId_BuildsMessageAndProperties()
    {
        var ex = new NotFoundException("User", 42);
        Assert.Equal("User with ID '42' was not found.", ex.Message);
        Assert.Equal("User", ex.ResourceName);
        Assert.Equal(42, ex.ResourceId);
    }

    [Fact]
    public void ResourceConstructor_WithoutId_BuildsMessage()
    {
        var ex = new NotFoundException("User", (object?)null);
        Assert.Equal("User was not found.", ex.Message);
        Assert.Null(ex.ResourceId);
    }
}

public class ConflictExceptionTests
{
    [Fact]
    public void Default_Has409AndDefaultMessage()
    {
        var ex = new ConflictException();
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("A conflict occurred.", ex.Message);
    }

    [Fact]
    public void ResourceConstructor_WithId_BuildsMessageAndProperties()
    {
        var ex = new ConflictException("Order", "abc");
        Assert.Equal("Order with ID 'abc' already exists or conflicts with existing data.", ex.Message);
        Assert.Equal("Order", ex.ResourceName);
        Assert.Equal("abc", ex.ResourceId);
    }

    [Fact]
    public void ResourceConstructor_WithoutId_BuildsMessage()
    {
        var ex = new ConflictException("Order", (object?)null);
        Assert.Equal("Order conflicts with existing data.", ex.Message);
    }

    [Fact]
    public void ToString_IncludesResourceInfo()
    {
        var ex = new ConflictException("Order", "abc");
        Assert.Contains("(Resource: Order, ID: abc)", ex.ToString(), StringComparison.Ordinal);
    }
}

public class ForbiddenExceptionTests
{
    [Fact]
    public void Default_Has403AndDefaultMessage()
    {
        var ex = new ForbiddenException();
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("Access to this resource is forbidden.", ex.Message);
    }

    [Fact]
    public void ResourceConstructor_WithIdAndReason_BuildsMessageAndProperties()
    {
        var ex = new ForbiddenException("Report", 7, "Insufficient role.");
        Assert.Equal("Access to Report with ID '7' is forbidden. Reason: Insufficient role.", ex.Message);
        Assert.Equal("Report", ex.ResourceName);
        Assert.Equal(7, ex.ResourceId);
        Assert.Equal("Insufficient role.", ex.Reason);
    }

    [Fact]
    public void ResourceConstructor_WithoutIdOrReason_BuildsMessage()
    {
        var ex = new ForbiddenException("Report", (object?)null);
        Assert.Equal("Access to Report is forbidden.", ex.Message);
        Assert.Null(ex.Reason);
    }
}

public class UnauthorizedExceptionTests
{
    [Fact]
    public void Default_Has401AndDefaultMessage()
    {
        var ex = new UnauthorizedException();
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("Authentication is required.", ex.Message);
        Assert.Null(ex.Reason);
    }

    [Fact]
    public void ReasonConstructor_IncludesReasonInMessage()
    {
        var ex = new UnauthorizedException("Token expired.", true);
        Assert.Equal("Authentication is required. Reason: Token expired.", ex.Message);
        Assert.Equal("Token expired.", ex.Reason);
    }

    [Fact]
    public void ReasonConstructor_ExcludesReasonFromMessageWhenRequested()
    {
        var ex = new UnauthorizedException("Token expired.", false);
        Assert.Equal("Authentication is required.", ex.Message);
        Assert.Equal("Token expired.", ex.Reason);
    }
}

public class RateLimitExceededExceptionTests
{
    [Fact]
    public void Default_Has429AndDefaultMessage()
    {
        var ex = new RateLimitExceededException();
        Assert.Equal(429, ex.StatusCode);
        Assert.Equal("Rate limit has been exceeded.", ex.Message);
        Assert.Null(ex.RetryAfter);
    }

    [Fact]
    public void RateLimitConstructor_AllValues_BuildsMessageAndProperties()
    {
        var ex = new RateLimitExceededException(TimeSpan.FromSeconds(30), 100, TimeSpan.FromSeconds(60));
        Assert.Equal(TimeSpan.FromSeconds(30), ex.RetryAfter);
        Assert.Equal(100, ex.RateLimit);
        Assert.Equal(TimeSpan.FromSeconds(60), ex.RateLimitWindow);
        Assert.Contains("Limit: 100 requests per 60 seconds.", ex.Message, StringComparison.Ordinal);
        Assert.Contains("retry after 30 seconds", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RateLimitConstructor_OnlyLimit_BuildsMessage()
    {
        var ex = new RateLimitExceededException(null, 50);
        Assert.Contains("Limit: 50 requests.", ex.Message, StringComparison.Ordinal);
    }
}

public class ServiceUnavailableExceptionTests
{
    [Fact]
    public void Default_Has503AndDefaultMessage()
    {
        var ex = new ServiceUnavailableException();
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("The service is temporarily unavailable.", ex.Message);
    }

    [Fact]
    public void ServiceConstructor_WithRetryAfter_BuildsMessageAndProperties()
    {
        var ex = new ServiceUnavailableException("Billing", TimeSpan.FromSeconds(15));
        Assert.Equal("Billing", ex.ServiceName);
        Assert.Equal(TimeSpan.FromSeconds(15), ex.RetryAfter);
        Assert.Equal("The service 'Billing' is temporarily unavailable. Please retry after 15 seconds.", ex.Message);
    }

    [Fact]
    public void ServiceConstructor_WithoutRetryAfter_BuildsMessage()
    {
        var ex = new ServiceUnavailableException("Billing", retryAfter: null);
        Assert.Equal("The service 'Billing' is temporarily unavailable.", ex.Message);
        Assert.Null(ex.RetryAfter);
    }
}

public class ValidationExceptionTests
{
    [Fact]
    public void Default_HasDefaultMessageAndNoErrors()
    {
        var ex = new ValidationException();
        Assert.Equal("Validation failed.", ex.Message);
        Assert.Empty(ex.Errors);
    }

    [Fact]
    public void SingleFieldConstructor_PopulatesErrorsAndMessage()
    {
        var ex = new ValidationException("Email", "Email is required.");
        Assert.StartsWith("Validation failed for field 'Email': Email is required.", ex.Message, StringComparison.Ordinal);
        Assert.Contains("- Email: Email is required.", ex.Message, StringComparison.Ordinal);
        Assert.Equal(["Email is required."], ex.Errors["Email"]);
    }

    [Fact]
    public void ErrorsConstructor_AppendsErrorDetailsToMessage()
    {
        var errors = new Dictionary<string, IReadOnlyList<string>> { ["Name"] = ["Name is required.", "Name is too long."], ["Age"] = ["Age must be positive."] };
        var ex = new ValidationException(errors);
        Assert.Equal(2, ex.Errors.Count);
        Assert.Contains("Validation errors:", ex.Message, StringComparison.Ordinal);
        Assert.Contains("- Name: Name is required.", ex.Message, StringComparison.Ordinal);
        Assert.Contains("- Age: Age must be positive.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorsConstructor_WithMessage_UsesMessage()
    {
        var errors = new Dictionary<string, IReadOnlyList<string>> { ["X"] = ["bad"] };
        var ex = new ValidationException(errors, "Custom validation failure.");
        Assert.StartsWith("Custom validation failure.", ex.Message, StringComparison.Ordinal);
        Assert.Single(ex.Errors);
    }

    [Fact]
    public void MessageAndInnerConstructor_SetsBoth()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new ValidationException("failed", inner);
        Assert.Same(inner, ex.InnerException);
        Assert.Empty(ex.Errors);
    }
}

public class ArgumentOutsideRangeExceptionTests
{
    [Fact]
    public void DefaultMessage_IncludesActualAndRange()
    {
        var ex = new ArgumentOutsideRangeException("count", 5, 1, 3);
        Assert.Equal("count", ex.ParamName);
        Assert.Equal(5, ex.ActualValue);
        Assert.Equal(1, ex.MinValue);
        Assert.Equal(3, ex.MaxValue);
        Assert.Contains("Value (5) is not in the allowed range [1, 3].", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullBounds_UseUnspecifiedInMessage()
    {
        var ex = new ArgumentOutsideRangeException("count", null, null, null);
        Assert.Contains("Value (NULL) is not in the allowed range [Unspecified, Unspecified].", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomMessage_IsUsed()
    {
        var ex = new ArgumentOutsideRangeException("count", 5, 1, 3, "Count out of bounds.");
        Assert.Contains("Count out of bounds.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_IncludesActualAndRange()
    {
        var ex = new ArgumentOutsideRangeException("count", 5, 1, 3);
        Assert.Contains("(Actual: 5, Range: [1, 3])", ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void IsArgumentOutOfRangeException() => Assert.IsAssignableFrom<ArgumentOutOfRangeException>(new ArgumentOutsideRangeException("p", 1, 2, 3));
}

public class InvalidFormatExceptionTests
{
    [Fact]
    public void Message_AppendsInvalidValueAndSingleFormat()
    {
        var ex = new InvalidFormatException("Bad color.", "color", "zzz", "Hex color");
        Assert.Equal("color", ex.ParamName);
        Assert.Equal("zzz", ex.InvalidValue);
        Assert.Contains("Bad color.", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Invalid value: 'zzz'.", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Valid format: Hex color.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Message_MultipleFormats_UsesPluralWording()
    {
        var ex = new InvalidFormatException("Bad value.", (string?)null, "x", "Format A", "Format B");
        Assert.Contains("Valid formats: Format A, Format B.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidFormats_FiltersBlankEntries()
    {
        var ex = new InvalidFormatException("Bad value.", (string?)null, null, "Format A", "", "  ");
        Assert.Equal(["Format A"], ex.ValidFormats);
    }

    [Fact]
    public void Message_NoInvalidValueOrFormats_IsBaseMessage()
    {
        var ex = new InvalidFormatException("Bad value.");
        Assert.Equal("Bad value.", ex.Message);
    }

    [Fact]
    public void InnerExceptionConstructor_SetsInner()
    {
        var inner = new FormatException("cause");
        var ex = new InvalidFormatException("Bad value.", inner, "param", "v");
        Assert.Same(inner, ex.InnerException);
        Assert.Equal("param", ex.ParamName);
        Assert.Equal("v", ex.InvalidValue);
    }

    [Fact]
    public void ToString_IncludesInvalidValueAndFormats()
    {
        var ex = new InvalidFormatException("Bad value.", (string?)null, "v", "Format A");
        Assert.Contains("(Invalid Value: 'v', Valid Formats: [Format A])", ex.ToString(), StringComparison.Ordinal);
    }
}

public class HttpExceptionTests
{
    [Fact]
    public void SubclassesExposeStatusCode()
    {
        HttpException ex = new NotFoundException();
        Assert.Equal(404, ex.StatusCode);
        ex = new ConflictException();
        Assert.Equal(409, ex.StatusCode);
        ex = new ForbiddenException();
        Assert.Equal(403, ex.StatusCode);
        ex = new UnauthorizedException();
        Assert.Equal(401, ex.StatusCode);
        ex = new RateLimitExceededException();
        Assert.Equal(429, ex.StatusCode);
        ex = new ServiceUnavailableException();
        Assert.Equal(503, ex.StatusCode);
    }
}