using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Billing.Postgres.Database;

/// <summary>EntityRef link from a billing client to a party.</summary>
public sealed class BillingContactEntity : EntityRelationEntityBase
{
    public Guid ClientId { get; set; }

    public int Role { get; set; }

    public string? Title { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsBillingContact { get; set; }

    public BillingClientEntity Client { get; set; } = null!;
}

/// <summary>EntityRef link from a billing client to a phone payload.</summary>
public sealed class BillingPhoneEntity : EntityRelationEntityBase
{
    public Guid ClientId { get; set; }

    public int Usage { get; set; }

    public bool IsPrimary { get; set; }

    public string? ContactEntityType { get; set; }

    public string? ContactEntityId { get; set; }

    public BillingClientEntity Client { get; set; } = null!;
}

/// <summary>EntityRef link from a billing client to an email payload.</summary>
public sealed class BillingEmailEntity : EntityRelationEntityBase
{
    public Guid ClientId { get; set; }

    public int Usage { get; set; }

    public bool IsPrimary { get; set; }

    public string? ContactEntityType { get; set; }

    public string? ContactEntityId { get; set; }

    public BillingClientEntity Client { get; set; } = null!;
}

/// <summary>EntityRef link from a billing client to an address payload.</summary>
public sealed class BillingAddressEntity : EntityRelationEntityBase
{
    public Guid ClientId { get; set; }

    public int Usage { get; set; }

    public bool IsPrimary { get; set; }

    public string? ContactEntityType { get; set; }

    public string? ContactEntityId { get; set; }

    public BillingClientEntity Client { get; set; } = null!;
}
