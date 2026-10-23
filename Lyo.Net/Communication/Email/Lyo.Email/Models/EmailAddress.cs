using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>An email address and an optional display name.</summary>
/// <param name="Address">Mailbox address.</param>
/// <param name="Name">Display name. Null when the caller did not set one.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailAddress(string Address, string? Name = null)
{
    /// <summary>Uses <paramref name="address" /> as the mailbox and leaves the display name unset.</summary>
    /// <param name="address">Mailbox address.</param>
    public static implicit operator EmailAddress(string address) => new(address);

    /// <summary>The mailbox address, so recipient lists stay readable when joined.</summary>
    /// <returns>The mailbox address.</returns>
    public override string ToString() => Address;
}
