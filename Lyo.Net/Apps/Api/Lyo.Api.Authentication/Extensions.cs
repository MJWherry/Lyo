using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Services.Users;
using Lyo.Authentication.Models;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Request;
using Lyo.Authentication.Models.Response;
using Lyo.Authentication.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Authentication;

/// <summary>Maps authentication admin HTTP routes, each surface gated by <see cref="EndpointAuth" /> (default: require authorization).</summary>
public static class Extensions
{
    private static readonly string[] TokenDeniedSelectFields = ["SecretHash"];

    private static readonly ApiFeatureSet UserFeatures = ApiFeatureSet.ReadOnly + ApiFeature.Patch + ExportApiFeature.Instance;

    private static readonly ApiFeatureSet TokenFeatures = ApiFeatureSet.ReadOnly + ApiFeature.Patch + ApiFeature.Delete + ApiFeature.DeleteBulk;

    /// <summary>Adds CRUD/export services for <see cref="UserDbContext" /> and the <see cref="AuthenticationLyoMapper" /> singleton. Does not register <c>ILyoMapper</c>.</summary>
    public static IServiceCollection AddLyoApiAuthentication(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddLyoCrudServices<UserDbContext>();
        services.AddLyoApiExport<UserDbContext>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<AuthenticationLyoMapper>();
        return services;
    }

    /// <summary>
    /// Maps User (Query/Get/Patch + Export), Token (Query/Get/Patch/Delete), Claim/Scope CRUD, and read-only LinkedIdentity/Event. Call after
    /// <see cref="AddLyoApiAuthentication" /> and Postgres auth stores. Defaults to <see cref="EndpointAuth.RequireAuthorization()" />.
    /// </summary>
    public static WebApplication BuildAuthenticationApi(this WebApplication app, AuthenticationApiOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(app);
        options ??= new();

        app.CreateBuilder<UserDbContext, UserEntity, AuthUserReq, AuthUserRes, Guid>(Constants.Rest.Auth.User, "Auth")
            .WithCrud(
                UserFeatures, new() {
                    QueryAuth = options.UserAuth,
                    GetAuth = options.UserAuth,
                    PatchAuth = options.UserAuth,
                    ExportAuth = options.UserAuth,
                    PatchPropertyAuthorization = AuthPatchPropertyRules.Block("Id", "CreatedTimestamp", "ScopesJson"),
                    BeforePatch = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow
                })
            .Build();

        app.CreateBuilder<UserDbContext, TokenEntity, AuthTokenReq, AuthTokenRes, string>(Constants.Rest.Auth.Token, "Auth")
            .WithCrud(
                TokenFeatures, new() {
                    QueryAuth = options.TokenAuth,
                    GetAuth = options.TokenAuth,
                    PatchAuth = options.TokenAuth,
                    DeleteAuth = options.TokenAuth,
                    DeleteBulkAuth = options.TokenAuth,
                    DeniedSelectFields = TokenDeniedSelectFields,
                    PatchPropertyAuthorization = AuthPatchPropertyRules.Block("Id", "CreatedTimestamp", "SecretHash"),
                    BeforePatch = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    AfterDelete = ctx => RecordTokenDeleted(ctx.Services, ctx.Entity)
                })
            .Build();

        app.CreateBuilder<UserDbContext, UserClaimEntity, AuthClaimReq, AuthClaimRes, Guid>(Constants.Rest.Auth.Claim, "Auth")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    QueryAuth = options.ClaimAuth,
                    GetAuth = options.ClaimAuth,
                    CreateAuth = options.ClaimAuth,
                    CreateBulkAuth = options.ClaimAuth,
                    UpdateAuth = options.ClaimAuth,
                    UpdateBulkAuth = options.ClaimAuth,
                    PatchAuth = options.ClaimAuth,
                    PatchBulkAuth = options.ClaimAuth,
                    UpsertAuth = options.ClaimAuth,
                    UpsertBulkAuth = options.ClaimAuth,
                    DeleteAuth = options.ClaimAuth,
                    DeleteBulkAuth = options.ClaimAuth,
                    PatchPropertyAuthorization = AuthPatchPropertyRules.Block("Id", "CreatedTimestamp"),
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = ctx.Entity.Id == Guid.Empty ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id;
                        ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                        AuthClaimWriteValidator.Validate(ctx.Entity);
                    },
                    BeforeUpdate = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        AuthClaimWriteValidator.Validate(ctx.Entity);
                    },
                    BeforePatch = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        AuthClaimWriteValidator.Validate(ctx.Entity);
                    }
                })
            .Build();

        app.CreateBuilder<UserDbContext, UserScopeEntity, AuthScopeReq, AuthScopeRes, Guid>(Constants.Rest.Auth.Scope, "Auth")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    QueryAuth = options.ScopeAuth,
                    GetAuth = options.ScopeAuth,
                    CreateAuth = options.ScopeAuth,
                    CreateBulkAuth = options.ScopeAuth,
                    UpdateAuth = options.ScopeAuth,
                    UpdateBulkAuth = options.ScopeAuth,
                    PatchAuth = options.ScopeAuth,
                    PatchBulkAuth = options.ScopeAuth,
                    UpsertAuth = options.ScopeAuth,
                    UpsertBulkAuth = options.ScopeAuth,
                    DeleteAuth = options.ScopeAuth,
                    DeleteBulkAuth = options.ScopeAuth,
                    PatchPropertyAuthorization = AuthPatchPropertyRules.Block("Id", "CreatedTimestamp"),
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = ctx.Entity.Id == Guid.Empty ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id;
                        ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                        AuthScopeWriteValidator.Validate(ctx.Entity);
                        EnsureUniqueScopeName(ctx.DbContext, ctx.Entity);
                    },
                    BeforeUpdate = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        AuthScopeWriteValidator.Validate(ctx.Entity);
                        EnsureUniqueScopeName(ctx.DbContext, ctx.Entity);
                    },
                    BeforePatch = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        AuthScopeWriteValidator.Validate(ctx.Entity);
                        EnsureUniqueScopeName(ctx.DbContext, ctx.Entity);
                    },
                    AfterCreateAsync = ctx => MirrorUserScopes(ctx.Services, ctx.Entity.UserId),
                    AfterUpdate = ctx => MirrorUserScopes(ctx.Services, ctx.Entity.UserId).GetAwaiter().GetResult(),
                    AfterPatch = ctx => MirrorUserScopes(ctx.Services, ctx.Entity.UserId).GetAwaiter().GetResult(),
                    AfterDelete = ctx => MirrorUserScopes(ctx.Services, ctx.Entity.UserId).GetAwaiter().GetResult()
                })
            .Build();

        app.CreateReadOnlyBuilder<UserDbContext, LinkedIdentityEntity, AuthLinkedIdentityRes, Guid>(Constants.Rest.Auth.LinkedIdentity, "Auth")
            .WithCrud(ApiFeatureSet.ReadOnly, new() { QueryAuth = options.LinkedIdentityAuth, GetAuth = options.LinkedIdentityAuth })
            .Build();

        app.CreateReadOnlyBuilder<UserDbContext, UserEventEntity, AuthEventRes, Guid>(Constants.Rest.Auth.Event, "Auth")
            .WithCrud(ApiFeatureSet.ReadOnly, new() { QueryAuth = options.EventAuth, GetAuth = options.EventAuth })
            .Build();

        return app;
    }

    private static void RecordTokenDeleted(IServiceProvider services, TokenEntity entity)
    {
        var audit = services.GetService<IAuthAuditRecorder>();
        if (audit is null)
            return;

        var accessor = services.GetService<IAuthAuditContextAccessor>();
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Lyo.Api.Authentication");
        _ = audit.RecordAsync(accessor, logger, AuthAuditEventKind.TokenDeleted, entity.UserId, entity.Id, outcome: "success");
    }

    private static Task MirrorUserScopes(IServiceProvider services, Guid userId)
    {
        var users = services.GetService<IUserStore>();
        var scopes = services.GetService<IUserScopeStore>();
        if (users is null || scopes is null)
            return Task.CompletedTask;

        return UserScopeNames.MirrorToUserAsync(users, scopes, userId, null);
    }

    private static void EnsureUniqueScopeName(UserDbContext db, UserScopeEntity entity)
    {
        var taken = db.Scopes.Any(s => s.UserId == entity.UserId && s.Name == entity.Name && s.Id != entity.Id);
        if (taken)
            throw new ConflictException($"Scope '{entity.Name}' is already granted to user '{entity.UserId}'.");
    }
}
