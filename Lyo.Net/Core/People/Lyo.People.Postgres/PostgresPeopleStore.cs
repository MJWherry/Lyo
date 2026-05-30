using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
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
        var entity = await context.Persons.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : PeopleEntityMapper.ToPerson(entity);
    }

    /// <inheritdoc />
    public async Task<Person?> GetPersonBySourceAsync(EntityRef source, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Persons.AsNoTracking()
            .FirstOrDefaultAsync(p => p.SourceEntityType == source.EntityType && p.SourceEntityId == source.EntityId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : PeopleEntityMapper.ToPerson(entity);
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
            entity = await context.Persons.FirstOrDefaultAsync(p => p.Id == person.Id, ct).ConfigureAwait(false) ?? mapped;
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
        EntitySourceMapping.ApplySource(entity, person.Source);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
