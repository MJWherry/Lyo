using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Ledger.Postgres.Database;

internal sealed class LedgerAccountEntityConfiguration : IEntityTypeConfiguration<LedgerAccountEntity>
{
    public void Configure(EntityTypeBuilder<LedgerAccountEntity> builder)
    {
        builder.ToTable("account");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Code).HasMaxLength(32).IsRequired().HasColumnName("code");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(e => e.AccountType).HasColumnName("account_type");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique().HasDatabaseName("uq_ledger_account_tenant_code");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_ledger_account_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}

internal sealed class LedgerJournalEntityConfiguration : IEntityTypeConfiguration<LedgerJournalEntity>
{
    public void Configure(EntityTypeBuilder<LedgerJournalEntity> builder)
    {
        builder.ToTable("journal");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Memo).HasMaxLength(4000).HasColumnName("memo");
        builder.Property(e => e.SourceEntityType).HasMaxLength(200).HasColumnName("source_entity_type");
        builder.Property(e => e.SourceEntityId).HasMaxLength(200).HasColumnName("source_entity_id");
        builder.Property(e => e.VoidOfJournalId).HasColumnName("void_of_journal_id").HasColumnType("uuid");
        builder.Property(e => e.PostedAt).HasColumnType("timestamp with time zone").HasColumnName("posted_at");
        builder.Property(e => e.VoidedAt).HasColumnType("timestamp with time zone").HasColumnName("voided_at");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.HasOne(e => e.VoidOf).WithMany(e => e.Reversals).HasForeignKey(e => e.VoidOfJournalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_ledger_journal_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_ledger_journal_status");
    }
}

internal sealed class LedgerJournalLineEntityConfiguration : IEntityTypeConfiguration<LedgerJournalLineEntity>
{
    public void Configure(EntityTypeBuilder<LedgerJournalLineEntity> builder)
    {
        builder.ToTable("journal_line");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.JournalId).IsRequired().HasColumnName("journal_id").HasColumnType("uuid");
        builder.Property(e => e.LineNumber).HasColumnName("line_number");
        builder.Property(e => e.AccountId).IsRequired().HasColumnName("account_id").HasColumnType("uuid");
        builder.Property(e => e.Debit).HasColumnType("numeric(18,4)").HasColumnName("debit");
        builder.Property(e => e.Credit).HasColumnType("numeric(18,4)").HasColumnName("credit");
        builder.Property(e => e.ClientId).HasColumnName("client_id").HasColumnType("uuid");
        builder.Property(e => e.Memo).HasMaxLength(1000).HasColumnName("memo");
        builder.HasOne(e => e.Journal).WithMany(e => e.Lines).HasForeignKey(e => e.JournalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Account).WithMany(e => e.Lines).HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.JournalId).HasDatabaseName("ix_ledger_journal_line_journal");
        builder.HasIndex(e => e.AccountId).HasDatabaseName("ix_ledger_journal_line_account");
        builder.HasIndex(e => e.ClientId).HasDatabaseName("ix_ledger_journal_line_client").HasFilter("\"client_id\" IS NOT NULL");
    }
}
