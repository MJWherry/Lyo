using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Format;
using Lyo.Authentication.Records;
using Lyo.Authentication.Services.Opaque;
using Lyo.Exceptions;

namespace Lyo.Authentication.Services.Refresh;

/// <summary>Default <see cref="ILyoRefreshTokenIssuer"/>. Thin wrapper over <see cref="IApiTokenIssuer"/>.</summary>
public sealed class DefaultLyoRefreshTokenIssuer : ILyoRefreshTokenIssuer
{
    /// <summary>Metadata key storing the originating provider name on a refresh token.</summary>
    public const string ProviderMetadataKey = "lyo_provider";

    /// <summary>Metadata key storing the IdP-issued <c>sub</c> claim on a refresh token.</summary>
    public const string ExternalSubjectMetadataKey = "lyo_external_sub";

    /// <summary>Metadata key storing the parent access-JWT's <c>jti</c>.</summary>
    public const string ParentJtiMetadataKey = "parent_jti";

    private readonly IApiTokenIssuer _issuer;

    /// <summary>Creates a new refresh-token issuer.</summary>
    public DefaultLyoRefreshTokenIssuer(IApiTokenIssuer issuer)
    {
        ArgumentHelpers.ThrowIfNull(issuer);
        _issuer = issuer;
    }

    /// <inheritdoc/>
    public Task<IssuedApiToken> IssueAsync(Guid userId, string parentJti, TimeSpan lifetime, string provider, string? externalSubject, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(parentJti);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        var metadata = new Dictionary<string, object?> {
            [ParentJtiMetadataKey] = parentJti,
            ["user_id"] = userId.ToString("D"),
            [ProviderMetadataKey] = provider
        };

        if (!string.IsNullOrWhiteSpace(externalSubject))
            metadata[ExternalSubjectMetadataKey] = externalSubject;

        var request = new ApiTokenIssueRequest(
            Kind: ApiTokenKind.Internal,
            DisplayName: "Lyo refresh token",
            Scopes: [LyoRefreshTokenScopes.Refresh],
            UserId: userId,
            Lifetime: lifetime,
            Metadata: metadata);

        return _issuer.IssueAsync(request, ct);
    }
}
