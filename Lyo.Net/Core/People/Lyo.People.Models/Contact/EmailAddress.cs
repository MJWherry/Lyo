namespace Lyo.People.Models.Contact;

/// <summary>Base email address model containing the actual email address data</summary>
public class EmailAddress : IEquatable<EmailAddress>
{
    /// <summary>Unique identifier for the email address</summary>
    public Guid Id { get; set; }

    /// <summary>Email address</summary>
    public string Email { get; set; } = null!;

    /// <summary>Whether the email address has been verified</summary>
    public bool IsVerified => VerifiedAt.HasValue;

    /// <summary>Date and time when the email address was verified</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Optional label or description for the address</summary>
    public string? Label { get; set; }

    /// <inheritdoc />
    public bool Equals(EmailAddress? other)
    {
        if (other == null)
            return false;

        return string.Equals(Email, other.Email, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EmailAddress other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Email?.ToLowerInvariant().GetHashCode() ?? 0;

    /// <summary>Equality operator</summary>
    public static bool operator ==(EmailAddress? left, EmailAddress? right) => Equals(left, right);

    /// <summary>Inequality operator</summary>
    public static bool operator !=(EmailAddress? left, EmailAddress? right) => !Equals(left, right);
}