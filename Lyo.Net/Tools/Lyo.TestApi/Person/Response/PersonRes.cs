namespace Lyo.TestApi.Person.Response;

public sealed record PersonRes
{
    public Guid Id { get; init; }

    public Guid? EndatoPersonId { get; init; }

    public DateTime CreatedTimestamp { get; init; }

    public DateTime? UpdatedTimestamp { get; init; }

    public DateTime? LocallyModifiedAt { get; init; }

    public string? CreatedBy { get; init; }

    public string? SourceEntityType { get; init; }

    public string? SourceEntityId { get; init; }

    public DateTime? ImportedAt { get; init; }

    public string? NamePrefix { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? NameSuffix { get; init; }

    public string? PreferredName { get; init; }

    public string? MaidenName { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public string? Sex { get; init; }

    public string? Nationality { get; init; }

    public string? PreferredLanguageBcp47 { get; init; }

    public string? Race { get; init; }

    public string? MaritalStatus { get; init; }

    public string? DisabilityStatus { get; init; }

    public string? VeteranStatus { get; init; }

    public Guid? PlaceOfBirthAddressId { get; init; }

    public Guid? EmergencyContactPersonId { get; init; }

    public string? CurrentJobTitle { get; init; }

    public string? CurrentCompany { get; init; }

    public bool IsActive { get; init; }

    public string? Notes { get; init; }

    public string? CitizenshipJson { get; init; }

    public string? PreferencesJson { get; init; }

    public string? CustomFieldsJson { get; init; }

    public IReadOnlyList<PersonAddressRes>? Addresses { get; init; }

    public IReadOnlyList<PersonEmailAddressRes>? EmailAddresses { get; init; }

    public IReadOnlyList<PersonPhoneNumberRes>? PhoneNumbers { get; init; }

    public PersonAddressRes? MostRecentAddress
        => Addresses?.OrderByDescending(a => a.UpdatedTimestamp ?? a.CreatedTimestamp).FirstOrDefault();

    public string FullName => $"{FirstName}{(string.IsNullOrEmpty(MiddleName) ? " " : $" {MiddleName} ")}{LastName}";
}
