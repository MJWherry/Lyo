using Lyo.Api.ApiEndpoint;
using Lyo.Authentication.Postgres.Database;
using Lyo.Exceptions.Models;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Api.Authentication.Tests;

public sealed class AuthClaimWriteValidatorTests
{
    [Fact]
    public void Validate_ReservedType_Throws()
    {
        var entity = new UserClaimEntity { UserId = LyoGuid.CreateV7(), Type = "scope", Value = "x" };
        var ex = Assert.Throws<ValidationException>(() => AuthClaimWriteValidator.Validate(entity));
        Assert.Contains("Type", ex.Errors.Keys);
    }

    [Fact]
    public void Validate_CustomType_Succeeds()
    {
        var entity = new UserClaimEntity { UserId = LyoGuid.CreateV7(), Type = "department", Value = "eng" };
        AuthClaimWriteValidator.Validate(entity);
    }
}
