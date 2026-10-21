using Lyo.Authentication.Saml;

namespace Lyo.Authentication.Okta;

/// <summary>Okta SAML identity provider. The subject is the NameID, or <see cref="OktaOptions.SubjectAttributeName" /> when that attribute is present.</summary>
public sealed class OktaSamlProvider(OktaOptions options) : ISamlIdentityProvider
{
    /// <inheritdoc />
    public string Name => OktaOptions.DefaultName;

    /// <inheritdoc />
    public string SpEntityId => options.EntityId ?? string.Empty;

    /// <inheritdoc />
    public string AcsUrl => options.AcsUrl ?? string.Empty;

    /// <inheritdoc />
    public string MetadataUrl => options.MetadataUrl;

    /// <inheritdoc />
    public string EmailAttributeName => options.EmailAttributeName;

    /// <inheritdoc />
    public string? SubjectAttributeName => options.SubjectAttributeName;
}