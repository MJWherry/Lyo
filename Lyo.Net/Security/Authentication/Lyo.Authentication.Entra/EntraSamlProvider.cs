using Lyo.Authentication.Saml;

namespace Lyo.Authentication.Entra;

/// <summary>Entra SAML identity provider. The subject is the object id attribute when the assertion includes it.</summary>
public sealed class EntraSamlProvider(EntraOptions options) : ISamlIdentityProvider
{
    /// <inheritdoc />
    public string Name => EntraOptions.DefaultName;

    /// <inheritdoc />
    public string SpEntityId => options.EntityId ?? string.Empty;

    /// <inheritdoc />
    public string AcsUrl => options.AcsUrl ?? string.Empty;

    /// <inheritdoc />
    public string MetadataUrl => options.MetadataUrl;

    /// <inheritdoc />
    public string EmailAttributeName => EntraOptions.EmailAttribute;

    /// <inheritdoc />
    public string? SubjectAttributeName => EntraOptions.ObjectIdAttribute;
}
