namespace Lyo.Authentication.Sso.Protocol;

/// <summary>HTTP method a protocol uses to finish login.</summary>
public enum SsoCallbackMethod
{
    /// <summary>OpenID Connect authorization-code callback.</summary>
    Get,

    /// <summary>SAML HTTP-POST assertion consumer.</summary>
    Post
}
