using System.ComponentModel.DataAnnotations;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Biller profile with encrypted remittance/bank account.</summary>
public sealed class BillerProfileEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(300)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? LegalName { get; set; }

    [MaxLength(4000)]
    public string? Remittance { get; set; }

    public byte[]? EncryptedRemittance { get; set; }

    [MaxLength(64)]
    public string? BankAccountNumber { get; set; }

    public byte[]? EncryptedBankAccountNumber { get; set; }

    [MaxLength(8)]
    public string? BankAccountLast4 { get; set; }
}
