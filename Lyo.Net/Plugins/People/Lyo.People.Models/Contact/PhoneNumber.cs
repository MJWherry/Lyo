using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.EntityReference.Models;

namespace Lyo.People.Models.Contact;

/// <summary>Canonical phone number payload (E.164, country, technology).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PhoneNumber : IEquatable<PhoneNumber>, IEntitySourceDerived
{
    /// <summary>Primary key for this number row.</summary>
    public Guid Id { get; set; }

    /// <summary>Number in E.164 form.</summary>
    public string Number { get; set; } = null!;

    /// <summary>Country of the number, when known.</summary>
    public CountryCode? CountryCode { get; set; }

    /// <summary>Dialing prefix as text (+1, +44, and so on) for display or parse.</summary>
    public string? CountryCodeString { get; set; }

    /// <summary>Technology (landline, mobile, VoIP).</summary>
    public PhoneType? TechnologyType { get; set; }

    /// <summary>True when <see cref="VerifiedAt" /> is set.</summary>
    public bool IsVerified => VerifiedAt.HasValue;

    /// <summary>When the number was verified.</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>True when the number can receive SMS (mobile only).</summary>
    public bool CanReceiveSms => TechnologyType is PhoneType.M;

    /// <summary>Optional caller-facing label.</summary>
    public string? Label { get; set; }

    /// <inheritdoc />
    public EntitySourceRecord? Source { get; set; }

    /// <inheritdoc />
    public DateTime? LocallyModifiedAt { get; set; }

    /// <inheritdoc />
    public bool Equals(PhoneNumber? other)
    {
        if (other == null)
            return false;

        return Number == other.Number && CountryCode == other.CountryCode;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PhoneNumber other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCodeHelpers.Combine(Number, CountryCode);

    /// <inheritdoc />
    public override string ToString() => $"PhoneNumber: id={Id}, number={Number}";

    /// <summary>Equality comparison.</summary>
    public static bool operator ==(PhoneNumber? left, PhoneNumber? right) => Equals(left, right);

    /// <summary>Inequality comparison.</summary>
    public static bool operator !=(PhoneNumber? left, PhoneNumber? right) => !Equals(left, right);
}
