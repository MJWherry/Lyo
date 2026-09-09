using Lyo.Api.ApiEndpoint;

namespace Lyo.Api.Authentication.Tests;

public sealed class TokenDeniedSelectTests
{
    [Fact]
    public void SecretHash_IsDenied()
    {
        var errors = DeniedSelectFieldPolicy.ValidateProjection(["Id", "SecretHash"], [], ["SecretHash"]);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void OtherFields_AreAllowed()
    {
        var errors = DeniedSelectFieldPolicy.ValidateProjection(["Id", "DisplayName", "Kind"], [], ["SecretHash"]);
        Assert.Empty(errors);
    }
}
