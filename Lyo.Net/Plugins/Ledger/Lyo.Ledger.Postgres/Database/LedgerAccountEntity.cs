using System.ComponentModel.DataAnnotations;

namespace Lyo.Ledger.Postgres.Database;

/// <summary>Chart-of-accounts row.</summary>
public sealed class LedgerAccountEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int AccountType { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<LedgerJournalLineEntity> Lines { get; set; } = [];
}
