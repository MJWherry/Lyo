using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.People.Models;
using Lyo.People.Postgres.Database;
using Lyo.People.Postgres.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Lyo.People.Postgres;

/// <summary>PostgreSQL implementation of <see cref="IPeopleStore" />.</summary>
public sealed class PostgresPeopleStore : IPeopleStore
{
    private readonly IDbContextFactory<PeopleDbContext> _contextFactory;

    public PostgresPeopleStore(IDbContextFactory<PeopleDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<Person?> GetPersonByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Persons.AsNoTracking().Include(p => p.Sources).FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : PeopleEntityMapper.ToPerson(entity);
    }

    /// <inheritdoc />
    public async Task<Person?> GetPersonBySourceAsync(EntityRef source, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sourceRow = await context.PersonSources.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceEntityType == source.EntityType && s.SourceEntityId == source.EntityId, ct)
            .ConfigureAwait(false);

        if (sourceRow == null)
            return null;

        return await GetPersonByIdAsync(sourceRow.PersonId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SavePersonAsync(Person person, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(person);
        ArgumentHelpers.ThrowIfNull(person.Name);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var mapped = PeopleEntityMapper.ToPersonEntity(person);
        PersonEntity entity;
        if (person.Id != default) {
            entity = await context.Persons.Include(p => p.Sources).FirstOrDefaultAsync(p => p.Id == person.Id, ct).ConfigureAwait(false) ?? mapped;
            if (entity.Id == mapped.Id)
                context.Entry(entity).CurrentValues.SetValues(mapped);
            else {
                context.Persons.Add(mapped);
                entity = mapped;
            }
        }
        else {
            context.Persons.Add(mapped);
            entity = mapped;
        }

        person.Id = entity.Id;
        context.PersonSources.RemoveRange(await context.PersonSources.Where(s => s.PersonId == entity.Id).ToListAsync(ct).ConfigureAwait(false));
        PeopleEntityMapper.ApplyPersonSources(entity, person.Sources);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}