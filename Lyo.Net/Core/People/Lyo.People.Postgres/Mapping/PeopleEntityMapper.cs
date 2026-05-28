using Lyo.Common.Enums;
using Lyo.Common.Extensions;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.People.Models;
using Lyo.People.Postgres.Database;

namespace Lyo.People.Postgres.Mapping;

internal static class PeopleEntityMapper
{
    public static Person ToPerson(PersonEntity entity)
    {
        var person = new Person {
            Id = entity.Id,
            Name = new PersonName {
                Prefix = ParseNamePrefix(entity.NamePrefix),
                FirstName = entity.FirstName,
                MiddleName = entity.MiddleName,
                LastName = entity.LastName,
                Suffix = ParseNameSuffix(entity.NameSuffix),
                PreferredName = entity.PreferredName,
                MaidenName = entity.MaidenName
            },
            DateOfBirth = entity.DateOfBirth,
            CreatedAt = entity.CreatedTimestamp,
            UpdatedAt = entity.UpdatedTimestamp,
            CreatedBy = entity.CreatedBy,
            IsActive = entity.IsActive,
            Notes = entity.Notes,
            CurrentJobTitle = entity.CurrentJobTitle,
            CurrentCompany = entity.CurrentCompany,
            PlaceOfBirthAddressId = entity.PlaceOfBirthAddressId,
            EmergencyContactPersonId = entity.EmergencyContactPersonId
        };

        foreach (var source in entity.Sources)
            person.Sources.Add(EntitySourceMapping.ToRecord(source));

        return person;
    }

    public static PersonEntity ToPersonEntity(Person person)
        => new() {
            Id = person.Id == default ? Guid.NewGuid() : person.Id,
            NamePrefix = person.Name.Prefix?.GetDescription(),
            FirstName = person.Name.FirstName,
            MiddleName = person.Name.MiddleName,
            LastName = person.Name.LastName,
            NameSuffix = person.Name.Suffix?.GetDescription(),
            PreferredName = person.Name.PreferredName,
            MaidenName = person.Name.MaidenName,
            DateOfBirth = person.DateOfBirth,
            CreatedTimestamp = person.CreatedAt == default ? DateTime.UtcNow : person.CreatedAt,
            UpdatedTimestamp = person.UpdatedAt,
            CreatedBy = person.CreatedBy,
            IsActive = person.IsActive,
            Notes = person.Notes,
            CurrentJobTitle = person.CurrentJobTitle,
            CurrentCompany = person.CurrentCompany,
            PlaceOfBirthAddressId = person.PlaceOfBirthAddressId,
            EmergencyContactPersonId = person.EmergencyContactPersonId
        };

    public static void ApplyPersonSources(PersonEntity entity, IEnumerable<EntitySourceRecord> sources)
    {
        EntitySourceMapping.SyncSources(
            entity.Sources,
            sources,
            entity.Id,
            parentId => new PersonSourceEntity { PersonId = parentId });
    }

    private static NamePrefix? ParseNamePrefix(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Enum.TryParse<NamePrefix>(value, true, out var p) ? p : null;

    private static NameSuffix? ParseNameSuffix(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Enum.TryParse<NameSuffix>(value, true, out var s) ? s : null;
}
