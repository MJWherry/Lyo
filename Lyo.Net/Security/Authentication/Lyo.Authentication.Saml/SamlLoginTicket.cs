namespace Lyo.Authentication.Saml;

/// <summary>Server-side SP-initiated login ticket. The id travels in RelayState and is not a URL.</summary>
/// <param name="Id">Opaque ticket id, at most 80 bytes.</param>
/// <param name="Provider">Provider name that created the AuthnRequest.</param>
/// <param name="RequestId">AuthnRequest ID echoed as InResponseTo.</param>
/// <param name="ReturnUrl">Sanitized return target captured at login start.</param>
/// <param name="Mode">Delivery mode captured at start. SAML completion always uses the browser handoff.</param>
/// <param name="ExpiresAt">UTC instant after which the ticket is rejected.</param>
public sealed record SamlLoginTicket(string Id, string Provider, string RequestId, string ReturnUrl, string Mode, DateTime ExpiresAt);
