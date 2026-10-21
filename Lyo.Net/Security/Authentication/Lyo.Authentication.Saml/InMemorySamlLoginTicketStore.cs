using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Lyo.Authentication.Models.Format;
using Lyo.Common.Core.Security;
using Lyo.Exceptions;

namespace Lyo.Authentication.Saml;

/// <summary>In-memory <see cref="ISamlLoginTicketStore" />. One process, same limit as the handoff code store.</summary>
public sealed class InMemorySamlLoginTicketStore : ISamlLoginTicketStore
{
    private readonly ConcurrentDictionary<string, SamlLoginTicket> _tickets = new(StringComparer.Ordinal);
    private readonly Func<DateTime> _utcNow;

    /// <summary>Creates a store that reads <see cref="DateTime.UtcNow" />.</summary>
    public InMemorySamlLoginTicketStore()
        : this(() => DateTime.UtcNow) { }

    /// <summary>Creates a store that reads <paramref name="utcNow" />. Tests use this to expire a ticket.</summary>
    public InMemorySamlLoginTicketStore(Func<DateTime> utcNow)
    {
        ArgumentHelpers.ThrowIfNull(utcNow);
        _utcNow = utcNow;
    }

    /// <inheritdoc />
    public SamlLoginTicket Issue(string provider, string requestId, string returnUrl, string mode)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentHelpers.ThrowIfNull(returnUrl);
        var id = Base64Url.Encode(CryptographicRandom.GetBytes(16));
        if (Encoding.UTF8.GetByteCount(id) > 80)
            throw new InvalidOperationException("SAML RelayState ticket id exceeds 80 bytes.");

        var ticket = new SamlLoginTicket(id, provider, requestId, returnUrl, mode, _utcNow().Add(SamlConstants.TicketTtl));
        _tickets[id] = ticket;
        return ticket;
    }

    /// <inheritdoc />
    public SamlLoginTicket? Consume(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !_tickets.TryRemove(id, out var ticket))
            return null;

        var presented = Encoding.UTF8.GetBytes(id);
        var stored = Encoding.UTF8.GetBytes(ticket.Id);
        if (presented.Length != stored.Length || !CryptographicOperations.FixedTimeEquals(presented, stored))
            return null;

        return _utcNow() <= ticket.ExpiresAt ? ticket : null;
    }
}