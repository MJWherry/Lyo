using Lyo.EntityReference.Models;
using Lyo.People.Models;

namespace Lyo.People.Postgres;

/// <summary>Writes and reads people domain rows plus provenance source links.</summary>
public interface IPeopleStore
{
    Task<Person?> GetPersonByIdAsync(Guid id, CancellationToken ct = default);

    Task<Person?> GetPersonBySourceAsync(EntityRef source, CancellationToken ct = default);

    Task SavePersonAsync(Person person, CancellationToken ct = default);

    /// <summary>People whose display name matches <paramref name="term" />.</summary>
    Task<IReadOnlyList<Person>> SearchByDisplayNameAsync(string term, CancellationToken ct = default);
}