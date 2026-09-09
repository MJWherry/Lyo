using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Models.Request;
using Lyo.Authentication.Models.Response;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Json;
using Lyo.Query.Models.Common.Request;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.Authentication.Tests;

public sealed class AuthenticationApiPostgresTests
{
    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();

    private readonly AuthenticationApiFixture _fixture;

    public AuthenticationApiPostgresTests(AuthenticationApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task QueryProject_User_ReturnsRows()
    {
        var user = await SeedUserAsync();
        var request = new ProjectionQueryReq { Start = 0, Amount = 10, Select = ["Id", "DisplayName", "Email"] };
        var response = await _fixture.Client.PostAsJsonAsync($"{Constants.Rest.Auth.User}/QueryProject", request, JsonOptions, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Items!, row => row.GetProperty("Id").GetGuid() == user.Id);
    }

    [Fact]
    public async Task QueryProject_Token_SecretHash_IsRejected()
    {
        var request = new ProjectionQueryReq { Start = 0, Amount = 1, Select = ["Id", "SecretHash"] };
        var response = await _fixture.Client.PostAsJsonAsync($"{Constants.Rest.Auth.Token}/QueryProject", request, JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Token_Revokes()
    {
        var token = await SeedTokenAsync();
        var now = DateTime.UtcNow;
        var patch = PatchRequestBuilder.New().WithKey(token.Id).SetProperty("RevokedTimestamp", now).SetProperty("RevokedReason", "test").Build();
        var response = await _fixture.Client.PatchAsJsonAsync(Constants.Rest.Auth.Token, patch, JsonOptions, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var loaded = await _fixture.Services.GetRequiredService<IApiTokenStore>().GetByIdAsync(token.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded?.RevokedAt);
        Assert.Equal("test", loaded.RevokedReason);
    }

    [Fact]
    public async Task Delete_Token_RemovesRow()
    {
        var token = await SeedTokenAsync();
        var response = await _fixture.Client.DeleteAsync($"{Constants.Rest.Auth.Token}/{token.Id}", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        Assert.Null(await _fixture.Services.GetRequiredService<IApiTokenStore>().GetByIdAsync(token.Id, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Claim_Crud_AndReservedRejected()
    {
        var user = await SeedUserAsync();
        var created = await _fixture.Client.PostAsJsonAsync(
            Constants.Rest.Auth.Claim, new AuthClaimReq { UserId = user.Id, Type = "department", Value = "eng" }, JsonOptions, TestContext.Current.CancellationToken);
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<CreateResult<AuthClaimRes>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(createdBody?.Data);
        Assert.Equal("department", createdBody.Data.Type);

        var reserved = await _fixture.Client.PostAsJsonAsync(
            Constants.Rest.Auth.Claim, new AuthClaimReq { UserId = user.Id, Type = "scope", Value = "nope" }, JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, reserved.StatusCode);

        var listed = await _fixture.Client.PostAsJsonAsync(
            $"{Constants.Rest.Auth.Claim}/QueryConcrete", new QueryConcreteReq { Amount = 50 }, JsonOptions, TestContext.Current.CancellationToken);
        listed.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Scope_Crud_AndDuplicateRejected()
    {
        var user = await SeedUserAsync();
        var created = await _fixture.Client.PostAsJsonAsync(
            Constants.Rest.Auth.Scope, new AuthScopeReq { UserId = user.Id, Name = "people.read" }, JsonOptions, TestContext.Current.CancellationToken);
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<CreateResult<AuthScopeRes>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(createdBody?.Data);
        Assert.Equal("people.read", createdBody.Data.Name);

        var duplicate = await _fixture.Client.PostAsJsonAsync(
            Constants.Rest.Auth.Scope, new AuthScopeReq { UserId = user.Id, Name = "people.read" }, JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var empty = await _fixture.Client.PostAsJsonAsync(
            Constants.Rest.Auth.Scope, new AuthScopeReq { UserId = user.Id, Name = "  " }, JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task QueryProject_Event_Succeeds()
    {
        var request = new ProjectionQueryReq { Start = 0, Amount = 5, Select = ["Id", "Kind", "Timestamp"] };
        var response = await _fixture.Client.PostAsJsonAsync($"{Constants.Rest.Auth.Event}/QueryProject", request, JsonOptions, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<LyoUser> SeedUserAsync()
    {
        var users = _fixture.Services.GetRequiredService<IUserStore>();
        var user = new LyoUser(
            Guid.NewGuid(), "Api Test", $"api-{Guid.NewGuid():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
        return await users.CreateAsync(user, null, TestContext.Current.CancellationToken);
    }

    private async Task<ApiTokenRecord> SeedTokenAsync()
    {
        var owner = await SeedUserAsync();
        var store = _fixture.Services.GetRequiredService<IApiTokenStore>();
        var id = NewTokenId();
        var token = new ApiTokenRecord(
            id, TestData.Create(32, TestData.Seed + id.GetHashCode()), ApiTokenKind.Pat, ApiTokenRing.Live, owner.Id, "api-test", [], null, DateTime.UtcNow, null, null, null,
            null, null, null);
        await store.InsertAsync(token, null, TestContext.Current.CancellationToken);
        return token;
    }

    private static string NewTokenId()
    {
        const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
        var bytes = TestData.Create(11, Environment.TickCount);
        return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
    }
}
