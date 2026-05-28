using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Geolocation.Postgres.Database;

/// <summary>Provenance for a row in <see cref="AddressEntity" />.</summary>
public sealed class AddressSourceEntity : EntitySourceEntityBase
{
    public Guid AddressId { get; set; }

    public AddressEntity Address { get; set; } = null!;
}
