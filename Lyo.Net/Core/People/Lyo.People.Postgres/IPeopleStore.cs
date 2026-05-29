using Lyo.EntityReference.Models;
using Lyo.People.Models;

namespace Lyo.People;

/// <summary>Persists people domain data and provenance source rows.</summary>
public interface IPeopleStore
{
    Task<Person?> GetPersonByIdAsync(Guid id, CancellationToken ct = default);

    Task<Person?> GetPersonBySourceAsync(EntityRef source, CancellationToken ct = default);

    Task SavePersonAsync(Person person, CancellationToken ct = default);
}