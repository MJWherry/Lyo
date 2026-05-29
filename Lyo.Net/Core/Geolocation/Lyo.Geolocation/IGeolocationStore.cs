using Lyo.EntityReference.Models;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.Geolocation;

/// <summary>Persists canonical geolocation domain data (addresses and provenance).</summary>
public interface IGeolocationStore
{
    /// <summary>Gets an address by id.</summary>
    Task<Address?> GetAddressByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets an address that has the given import source.</summary>
    Task<Address?> GetBySourceAsync(EntityRef source, CancellationToken ct = default);

    /// <summary>Inserts or updates an address and its source rows.</summary>
    Task SaveAddressAsync(Address address, CancellationToken ct = default);
}