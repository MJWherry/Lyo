using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;

namespace Lyo.Authentication.Services.Refresh;

/// <summary>Shared matching for Format-B refresh tokens (login replacement and listing).</summary>
public static class LyoRefreshTokenMatch
{
    /// <summary>
    /// True when <paramref name="record" /> is a live <c>internal</c> refresh token for <paramref name="userId" />. When <paramref name="provider" /> is set, the
    /// token's <c>lyo_provider</c> metadata must match.
    /// </summary>
    public static bool IsLiveRefreshForUser(ApiTokenRecord record, Guid userId, string? provider)
    {
        ArgumentHelpers.ThrowIfNull(record);
        if (record.UserId != userId || record.RevokedAt.HasValue)
            return false;

        if (!string.Equals(record.Kind, ApiTokenKind.Internal, StringComparison.Ordinal))
            return false;

        var hasRefresh = false;
        foreach (var scope in record.Scopes) {
            if (string.Equals(scope, LyoRefreshTokenScopes.Refresh, StringComparison.Ordinal)) {
                hasRefresh = true;
                break;
            }
        }

        if (!hasRefresh)
            return false;

        if (provider.IsNullOrWhitespace())
            return true;

        if (record.Metadata is null || !record.Metadata.TryGetValue(DefaultLyoRefreshTokenIssuer.ProviderMetadataKey, out var raw))
            return false;

        return string.Equals(raw?.ToString(), provider, StringComparison.Ordinal);
    }
}
