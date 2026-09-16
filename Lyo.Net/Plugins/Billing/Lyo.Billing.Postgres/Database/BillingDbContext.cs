using Microsoft.EntityFrameworkCore;

namespace Lyo.Billing.Postgres.Database;

/// <summary>EF Core context for the <c>billing</c> schema.</summary>
public class BillingDbContext : DbContext
{
    /// <summary>Bill-to clients owned by billing.</summary>
    public DbSet<BillingClientEntity> Clients { get; set; } = null!;

    /// <summary>Unbilled and billed charges.</summary>
    public DbSet<ChargeEntity> Charges { get; set; } = null!;

    /// <summary>Invoices.</summary>
    public DbSet<InvoiceEntity> Invoices { get; set; } = null!;

    /// <summary>Invoice lines.</summary>
    public DbSet<InvoiceLineEntity> InvoiceLines { get; set; } = null!;

    /// <summary>Payments.</summary>
    public DbSet<PaymentEntity> Payments { get; set; } = null!;

    /// <summary>Biller profiles.</summary>
    public DbSet<BillerProfileEntity> BillerProfiles { get; set; } = null!;

    /// <summary>Invoice number sequences.</summary>
    public DbSet<InvoiceSequenceEntity> InvoiceSequences { get; set; } = null!;

    /// <summary>Lite person payloads.</summary>
    public DbSet<BillingPersonEntity> People { get; set; } = null!;

    /// <summary>Lite phone payloads.</summary>
    public DbSet<BillingPhoneNumberEntity> PhoneNumbers { get; set; } = null!;

    /// <summary>Lite email payloads.</summary>
    public DbSet<BillingEmailAddressEntity> EmailAddresses { get; set; } = null!;

    /// <summary>Lite address payloads.</summary>
    public DbSet<BillingAddressPayloadEntity> AddressPayloads { get; set; } = null!;

    /// <summary>Contact EntityRef links.</summary>
    public DbSet<BillingContactEntity> Contacts { get; set; } = null!;

    /// <summary>Phone EntityRef links.</summary>
    public DbSet<BillingPhoneEntity> Phones { get; set; } = null!;

    /// <summary>Email EntityRef links.</summary>
    public DbSet<BillingEmailEntity> Emails { get; set; } = null!;

    /// <summary>Address EntityRef links.</summary>
    public DbSet<BillingAddressEntity> Addresses { get; set; } = null!;

    /// <summary>Constructs the context.</summary>
    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresBillingOptions.Schema);
        modelBuilder.ApplyConfiguration(new BillingClientEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ChargeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceLineEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillerProfileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceSequenceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingPersonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingPhoneNumberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingEmailAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingAddressPayloadEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingContactEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingPhoneEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingEmailEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BillingAddressEntityConfiguration());
    }
}
