using Lyo.Authentication.Postgres.Database;
using Lyo.Exceptions.Models;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Api.Authentication.Tests;

public sealed class AuthScopeWriteValidatorTests
{
    [Fact]
    public void Validate_EmptyName_Throws()
    {
        var entity = new UserScopeEntity { UserId = LyoGuid.CreateV7(), Name = " " };
        var ex = Assert.Throws<ValidationException>(() => AuthScopeWriteValidator.Validate(entity));
        Assert.Contains("Name", ex.Errors.Keys);
    }

    [Fact]
    public void Validate_Name_Succeeds()
    {
        var entity = new UserScopeEntity { UserId = LyoGuid.CreateV7(), Name = " people.read " };
        AuthScopeWriteValidator.Validate(entity);
        Assert.Equal("people.read", entity.Name);
    }
}
