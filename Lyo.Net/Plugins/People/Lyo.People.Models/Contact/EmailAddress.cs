using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.People.Models.Contact;

/// <summary>Canonical email payload (address, verification, label).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class EmailAddress : IEquatable<EmailAddress>, IEntitySourceDerived
{
    /// <summary>Primary key for this mailbox row.</summary>
    public Guid Id { get; set; }

    /// <summary>Mailbox address.</summary>
    public string Email { get; set; } = null!;

    /// <summary>True when <see cref="VerifiedAt" /> is set.</summary>
    public bool IsVerified => VerifiedAt.HasValue;

    /// <summary>When the mailbox was verified.</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Optional caller-facing label.</summary>
    public string? Label { get; set; }

    /// <inheritdoc />
    public EntitySourceRecord? Source { get; set; }

    /// <inheritdoc />
    public DateTime? LocallyModifiedAt { get; set; }

    /// <inheritdoc />
    public bool Equals(EmailAddress? other) => other != null && string.Equals(Email, other.Email, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EmailAddress other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Email.ToLowerInvariant().GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"EmailAddress: id={Id}, email={Email}";

    /// <summary>Equality comparison.</summary>
    public static bool operator ==(EmailAddress? left, EmailAddress? right) => Equals(left, right);

    /// <summary>Inequality comparison.</summary>
    public static bool operator !=(EmailAddress? left, EmailAddress? right) => !Equals(left, right);
}
