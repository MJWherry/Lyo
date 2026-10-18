namespace Lyo.Authentication.Saml;

/// <summary>Loads <see cref="SamlIdpMetadata" /> for a provider. The default cache fetches HTTPS metadata.</summary>
public interface ISamlMetadataSource
{
    /// <summary>Returns metadata for <paramref name="provider" />. A failed refresh keeps the last good copy.</summary>
    Task<SamlIdpMetadata> GetAsync(ISamlIdentityProvider provider, CancellationToken ct = default);
}
