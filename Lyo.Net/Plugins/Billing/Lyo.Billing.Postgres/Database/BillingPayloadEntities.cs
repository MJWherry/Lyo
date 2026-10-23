using System.ComponentModel.DataAnnotations;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Lite person payload when People is not wired.</summary>
public sealed class BillingPersonEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(300)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? GivenName { get; set; }

    [MaxLength(100)]
    public string? FamilyName { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public DateTime CreatedTimestamp { get; set; }
}

/// <summary>Lite phone payload.</summary>
public sealed class BillingPhoneNumberEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(2)]
    public string? Country { get; set; }

    public DateTime CreatedTimestamp { get; set; }
}

/// <summary>Lite email payload.</summary>
public sealed class BillingEmailAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedTimestamp { get; set; }
}

/// <summary>Lite address payload.</summary>
public sealed class BillingAddressPayloadEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    [MaxLength(300)]
    public string? Line1 { get; set; }

    [MaxLength(300)]
    public string? Line2 { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(120)]
    public string? Region { get; set; }

    [MaxLength(32)]
    public string? PostalCode { get; set; }

    [MaxLength(2)]
    public string? Country { get; set; }

    public DateTime CreatedTimestamp { get; set; }
}