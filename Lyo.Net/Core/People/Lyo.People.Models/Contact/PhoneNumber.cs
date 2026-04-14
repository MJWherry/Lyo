using Lyo.Common.Enums;

namespace Lyo.People.Models.Contact;

/// <summary>Base phone number model containing the actual phone number data</summary>
public class PhoneNumber : IEquatable<PhoneNumber>
{
    /// <summary>Unique identifier for the phone number</summary>
    public Guid Id { get; set; }

    /// <summary>Phone number in E.164 format</summary>
    public string Number { get; set; } = null!;

    /// <summary>Country code enum</summary>
    public CountryCode? CountryCode { get; set; }

    /// <summary>Country code string (+1, +44, etc.) for display/parsing</summary>
    public string? CountryCodeString { get; set; }

    /// <summary>Technology type (Landline, Mobile, VoIP)</summary>
    public PhoneType? TechnologyType { get; set; }

    /// <summary>Whether the phone number has been verified</summary>
    public bool IsVerified => VerifiedAt.HasValue;

    /// <summary>Date and time when the phone number was verified</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Whether the phone number can receive SMS (Mobile only)</summary>
    public bool CanReceiveSms => TechnologyType is PhoneType.M;

    /// <summary>Optional label or description for the number</summary>
    public string? Label { get; set; }

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
    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Number?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (CountryCode?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    /// <summary>Equality operator</summary>
    public static bool operator ==(PhoneNumber? left, PhoneNumber? right) => Equals(left, right);

    /// <summary>Inequality operator</summary>
    public static bool operator !=(PhoneNumber? left, PhoneNumber? right) => !Equals(left, right);
}