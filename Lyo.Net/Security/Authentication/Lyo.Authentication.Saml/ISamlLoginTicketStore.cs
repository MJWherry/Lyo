namespace Lyo.Authentication.Saml;

/// <summary>Single-use store for <see cref="SamlLoginTicket" />. The first consume removes the ticket, including when validation later fails.</summary>
public interface ISamlLoginTicketStore
{
    /// <summary>Stores a new ticket and returns it. The id is at most 80 bytes.</summary>
    SamlLoginTicket Issue(string provider, string requestId, string returnUrl, string mode);

    /// <summary>Removes and returns the ticket, or null when it is missing, expired, or the id does not match.</summary>
    SamlLoginTicket? Consume(string id);
}
