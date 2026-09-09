using Lyo.EntityReference.Models;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.Geolocation;

/// <summary>Persists canonical geolocation data (addresses and provenance).</summary>
public interface IGeolocationStore
{
    /// <summary>Returns the address with the given id.</summary>
    Task<Address?> GetAddressByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the address that carries the given import source.</summary>
    Task<Address?> GetBySourceAsync(EntityRef source, CancellationToken ct = default);

    /// <summary>Inserts or overwrites an address and its source rows.</summary>
    Task SaveAddressAsync(Address address, CancellationToken ct = default);
}