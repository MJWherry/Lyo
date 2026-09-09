using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Tests;

public class LyoJwtClaimsTests
{
    [Theory]
    [InlineData("iss")]
    [InlineData("sub")]
    [InlineData("aud")]
    [InlineData("exp")]
    [InlineData("nbf")]
    [InlineData("iat")]
    [InlineData("jti")]
    [InlineData("scope")]
    [InlineData("lyo:user")]
    [InlineData("lyo:provider")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsReserved_ReservedNames_ReturnsTrue(string? type) => Assert.True(LyoJwtClaims.IsReserved(type));

    [Theory]
    [InlineData("department")]
    [InlineData("role")]
    [InlineData("custom")]
    public void IsReserved_CustomNames_ReturnsFalse(string type) => Assert.False(LyoJwtClaims.IsReserved(type));
}
