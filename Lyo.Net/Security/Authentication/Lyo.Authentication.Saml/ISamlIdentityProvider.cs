namespace Lyo.Authentication.Saml;

/// <summary>One SAML identity provider that this service provider accepts.</summary>
public interface ISamlIdentityProvider
{
    /// <summary>Provider name used in <c>/auth/login/{name}</c> and on the linked identity.</summary>
    string Name { get; }

    /// <summary>SP entity id. Must match the IdP app's audience.</summary>
    string SpEntityId { get; }

    /// <summary>Assertion consumer service URL. Must match the IdP reply URL.</summary>
    string AcsUrl { get; }

    /// <summary>HTTPS IdP metadata URL.</summary>
    string MetadataUrl { get; }

    /// <summary>Attribute <c>Name</c> that carries the verified email. Other attributes do not count as verified.</summary>
    string EmailAttributeName { get; }

    /// <summary>Attribute <c>Name</c> used as the stable subject when present. Otherwise the NameID is the subject.</summary>
    string? SubjectAttributeName { get; }
}