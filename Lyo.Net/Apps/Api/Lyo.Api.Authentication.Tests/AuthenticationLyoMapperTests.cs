using Lyo.Authentication.Models.Request;
using Lyo.Authentication.Models.Response;
using Lyo.Authentication.Postgres.Database;

namespace Lyo.Api.Authentication.Tests;

public sealed class AuthenticationLyoMapperTests
{
    private readonly AuthenticationLyoMapper _mapper = new();

    [Fact]
    public void Maps_User_Req_And_Res()
    {
        var req = new AuthUserReq {
            DisplayName = "Ada",
            Email = "ada@example.com",
            EmailVerified = true,
            ScopesJson = """["people.read"]""",
            DisabledReason = null
        };
        var entity = _mapper.Map<UserEntity>(req);
        entity.Id = Guid.NewGuid();
        entity.CreatedTimestamp = DateTime.UtcNow;
        var res = _mapper.Map<AuthUserRes>(entity);
        Assert.Equal("Ada", res.DisplayName);
        Assert.Equal("ada@example.com", res.Email);
        Assert.Equal(entity.Id, res.Id);
        Assert.DoesNotContain("SecretHash", typeof(AuthTokenRes).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void Maps_Token_Res_Without_Hash()
    {
        var entity = new TokenEntity {
            Id = "abcdefghjkm",
            SecretHash = [1, 2, 3],
            Kind = "pat",
            Ring = "live",
            DisplayName = "ci",
            ScopesJson = "[]",
            CreatedTimestamp = DateTime.UtcNow
        };
        var res = _mapper.Map<AuthTokenRes>(entity);
        Assert.Equal("abcdefghjkm", res.Id);
        Assert.Equal("ci", res.DisplayName);
        Assert.Null(typeof(AuthTokenRes).GetProperty("SecretHash"));
    }

    [Fact]
    public void Maps_Claim_Req()
    {
        var req = new AuthClaimReq { UserId = Guid.NewGuid(), Type = "department", Value = "eng" };
        var entity = _mapper.Map<UserClaimEntity>(req);
        Assert.Equal("department", entity.Type);
        Assert.Equal("eng", entity.Value);
        var res = _mapper.Map<AuthClaimRes>(entity);
        Assert.Equal("department", res.Type);
    }

    [Fact]
    public void Maps_Scope_Req()
    {
        var req = new AuthScopeReq { UserId = Guid.NewGuid(), Name = "people.read" };
        var entity = _mapper.Map<UserScopeEntity>(req);
        Assert.Equal("people.read", entity.Name);
        var res = _mapper.Map<AuthScopeRes>(entity);
        Assert.Equal("people.read", res.Name);
    }
}
