using Lyo.Api.Mapping;
using Lyo.Authentication.Models.Request;
using Lyo.Authentication.Models.Response;
using Lyo.Authentication.Postgres.Database;

namespace Lyo.Api.Authentication;

/// <summary>Hand-rolled <see cref="ILyoMapper" /> for authentication admin Req, entity, and Res types.</summary>
/// <remarks>
/// Register as a singleton and nest it first in the host's <see cref="ILyoMapper" /> composite (Security packages must not own <see cref="ILyoMapper" />). QueryProject selects
/// entity fields; Get/QueryConcrete use these Res mappings. Token Res never includes <c>SecretHash</c>.
/// </remarks>
public sealed class AuthenticationLyoMapper : ILyoMapper
{
    /// <inheritdoc />
    public TResult Map<TResult>(object source)
        => source switch {
            AuthUserReq req when typeof(TResult) == typeof(UserEntity) => (TResult)(object)ReqToNew(req),
            AuthTokenReq req when typeof(TResult) == typeof(TokenEntity) => (TResult)(object)ReqToNew(req),
            AuthClaimReq req when typeof(TResult) == typeof(UserClaimEntity) => (TResult)(object)ReqToNew(req),
            AuthScopeReq req when typeof(TResult) == typeof(UserScopeEntity) => (TResult)(object)ReqToNew(req),
            UserEntity e when typeof(TResult) == typeof(AuthUserRes) => (TResult)(object)ToRes(e),
            TokenEntity e when typeof(TResult) == typeof(AuthTokenRes) => (TResult)(object)ToRes(e),
            UserClaimEntity e when typeof(TResult) == typeof(AuthClaimRes) => (TResult)(object)ToRes(e),
            UserScopeEntity e when typeof(TResult) == typeof(AuthScopeRes) => (TResult)(object)ToRes(e),
            LinkedIdentityEntity e when typeof(TResult) == typeof(AuthLinkedIdentityRes) => (TResult)(object)ToRes(e),
            UserEventEntity e when typeof(TResult) == typeof(AuthEventRes) => (TResult)(object)ToRes(e),
            var _ => throw Unmapped(source.GetType(), typeof(TResult))
        };

    /// <inheritdoc />
    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (AuthUserReq req, UserEntity e):
                Apply(req, e);
                break;
            case (AuthTokenReq req, TokenEntity e):
                Apply(req, e);
                break;
            case (AuthClaimReq req, UserClaimEntity e):
                Apply(req, e);
                break;
            case (AuthScopeReq req, UserScopeEntity e):
                Apply(req, e);
                break;
            default:
                throw Unmapped(typeof(TSource), typeof(TDest));
        }
    }

    internal static UserEntity ReqToNew(AuthUserReq req)
    {
        var entity = new UserEntity();
        Apply(req, entity);
        return entity;
    }

    internal static void Apply(AuthUserReq req, UserEntity entity)
    {
        entity.DisplayName = req.DisplayName;
        entity.Email = req.Email;
        entity.EmailVerified = req.EmailVerified;
        entity.AvatarUrl = req.AvatarUrl;
        entity.PreferredLanguageBcp47 = req.PreferredLanguageBcp47;
        entity.ScopesJson = string.IsNullOrWhiteSpace(req.ScopesJson) ? "[]" : req.ScopesJson;
        entity.PersonId = req.PersonId;
        entity.DisabledTimestamp = req.DisabledTimestamp;
        entity.DisabledReason = req.DisabledReason;
    }

    internal static AuthUserRes ToRes(UserEntity e)
        => new(
            e.Id, e.DisplayName, e.Email, e.EmailVerified, e.AvatarUrl, e.PreferredLanguageBcp47, e.ScopesJson, e.PersonId, e.TenantId, e.CreatedTimestamp, e.UpdatedTimestamp,
            e.LastLoginTimestamp, e.DisabledTimestamp, e.DisabledReason);

    internal static TokenEntity ReqToNew(AuthTokenReq req)
    {
        var entity = new TokenEntity();
        Apply(req, entity);
        return entity;
    }

    internal static void Apply(AuthTokenReq req, TokenEntity entity)
    {
        entity.DisplayName = req.DisplayName;
        entity.RevokedTimestamp = req.RevokedTimestamp;
        entity.RevokedReason = req.RevokedReason;
    }

    internal static AuthTokenRes ToRes(TokenEntity e)
        => new(
            e.Id, e.Kind, e.Ring, e.UserId, e.TenantId, e.DisplayName, e.ScopesJson, e.MetadataJson, e.CreatedTimestamp, e.UpdatedTimestamp, e.ExpiresTimestamp,
            e.LastUsedTimestamp, e.RevokedTimestamp, e.RevokedReason, e.RotatedFromId);

    internal static UserClaimEntity ReqToNew(AuthClaimReq req)
    {
        var entity = new UserClaimEntity();
        Apply(req, entity);
        return entity;
    }

    internal static void Apply(AuthClaimReq req, UserClaimEntity entity)
    {
        entity.UserId = req.UserId;
        entity.Type = req.Type;
        entity.Value = req.Value;
    }

    internal static AuthClaimRes ToRes(UserClaimEntity e) => new(e.Id, e.UserId, e.Type, e.Value, e.CreatedTimestamp, e.UpdatedTimestamp);

    internal static UserScopeEntity ReqToNew(AuthScopeReq req)
    {
        var entity = new UserScopeEntity();
        Apply(req, entity);
        return entity;
    }

    internal static void Apply(AuthScopeReq req, UserScopeEntity entity)
    {
        entity.UserId = req.UserId;
        entity.Name = req.Name;
    }

    internal static AuthScopeRes ToRes(UserScopeEntity e) => new(e.Id, e.UserId, e.Name, e.CreatedTimestamp, e.UpdatedTimestamp);

    internal static AuthLinkedIdentityRes ToRes(LinkedIdentityEntity e)
        => new(
            e.Id, e.UserId, e.TenantId, e.Provider, e.Subject, e.EmailAtLink, e.ScopesJson, e.RawClaimsJson, e.LinkedTimestamp, e.UpdatedTimestamp, e.LastUsedTimestamp,
            e.UnlinkedTimestamp);

    internal static AuthEventRes ToRes(UserEventEntity e)
        => new(e.Id, e.Timestamp, e.Kind, e.UserId, e.TenantId, e.Subject, e.Provider, e.Outcome, e.Reason, e.IpAddress, e.UserAgent, e.CorrelationId, e.MetadataJson);

    private static InvalidOperationException Unmapped(Type source, Type dest) => new($"No mapping registered from {source.Name} to {dest.Name}.");
}
